// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

namespace FormFinch.JsonSchemaValidation.Formats;

internal static partial class ExtendedFormatValidators
{
    private const string SpacesDotsDashesAndSlashes = "  .-/";
    private const string SpacesDashesAndSlashes = "  -/";

    // Grobadressierung lengths allowed by the Leitweg-ID specification 2.0.2, and its Land codes.
    private static readonly Regex DeLeitwegRegex = ShapeRegex("^([0-9]{2,12})(?:-([0-9A-Z]{1,30}))?-([0-9]{2})$");
    private static readonly Regex DeTradeRegisterRegex = ShapeRegex("^(?:HRA|HRB|GNR|GSR|PR|VR)[1-9][0-9]{0,5}[A-Z]{0,3}$");
    private static readonly Regex DeWknRegex = ShapeRegex("^[0-9A-HJ-NP-Z]{6}$");
    private static readonly Regex DeDocumentRegex = ShapeRegex("^[0-9CFGHJKLMNPRTVWXYZ]{9}[0-9]?$");
    private static readonly Regex DeStnrLandRegex = ShapeRegex("^(?:10|11|21|22|23|24|26|27|28|30|31|32|40|41|5[0-9]|9[0-9])");
    private static readonly Regex DeStnrBezirkFrom100Regex = ShapeRegex("^(?:9|30|40|10|32|31|41)");

    // Kfz-Kennzeichen: letter run (district and Erkennung, optionally split by one separator
    // group), number 1-9999, optional E or H suffix. Letters include the umlauts of district codes.
    private static readonly Regex DePlateRegex = ShapeRegex(
        "^([A-ZÄÖÜ]+(?:[  -]+[A-ZÄÖÜ]+)*)[  -]*([1-9][0-9]{0,3})(?:[  ]*([EH]))?$");

    private static readonly int[] DeLeitwegGrobLengths = [2, 3, 5, 8, 9, 12];

    /// <summary>German phone number: 6–13 significant digits (mobile 15x: 11; 160/162/163/17x: 10–11) after the <c>0</c>, <c>+49</c> or <c>0049</c> prefix.</summary>
    /// <param name="value">The string to check. Spaces, dashes, dots, slashes and parentheses allowed, e.g. <c>+49 (0)30 1234567</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid German phone number.</returns>
    public static bool IsValidDePhone(string value)
    {
        if (!TryParsePhone(value, "49", out var national))
            return false;
        if (national.StartsWith("15", StringComparison.Ordinal))
            return national.Length == 11;
        if (national.StartsWith("160", StringComparison.Ordinal) || national.StartsWith("162", StringComparison.Ordinal)
            || national.StartsWith("163", StringComparison.Ordinal) || national.StartsWith("17", StringComparison.Ordinal))
            return national.Length is 10 or 11;
        return national.Length is >= 6 and <= 13;
    }

    /// <summary>German postcode (Postleitzahl): 5 digits, not starting with <c>00</c>.</summary>
    /// <param name="value">The string to check. No separators.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid German postcode.</returns>
    public static bool IsValidDePostcode(string value)
    {
        if (value is null)
            return false;
        var text = Trim(value);
        return text.Length == 5 && IsAllDigits(text) && !(text[0] == '0' && text[1] == '0');
    }

    /// <summary>German VAT identification number (USt-IdNr): <c>DE</c> + 9 digits with an ISO 7064 MOD 11,10 check digit.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid German VAT identification number.</returns>
    public static bool IsValidDeVat(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var vat) || vat.Length != 11
            || vat[0] != 'D' || vat[1] != 'E')
            return false;
        var digits = vat[2..];
        return IsAllDigits(digits) && digits[0] != '0' && PassesMod1110(digits);
    }

    /// <summary>German tax identification number (Steuer-IdNr): 11 digits, one repeated digit, MOD 11,10 check digit.</summary>
    /// <param name="value">The string to check. Spaces, dots, dashes and slashes allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid tax identification number.</returns>
    public static bool IsValidDeIdnr(string value)
    {
        if (!TryCompact(value, SpacesDotsDashesAndSlashes, allowLetters: false, out var idnr)
            || idnr.Length != 11 || idnr[0] == '0')
            return false;
        // Among the first 10 digits exactly one digit value occurs twice or three times; a
        // tripled digit may not stand three in a row.
        Span<int> counts = stackalloc int[10];
        for (int i = 0; i < 10; i++)
            counts[idnr[i] - '0']++;
        int repeated = -1;
        for (int d = 0; d < 10; d++)
        {
            if (counts[d] > 3)
                return false;
            if (counts[d] > 1)
            {
                if (repeated >= 0)
                    return false;
                repeated = d;
            }
        }
        if (repeated < 0)
            return false;
        if (counts[repeated] == 3)
        {
            char c = (char)('0' + repeated);
            for (int i = 0; i + 2 < 10; i++)
            {
                if (idnr[i] == c && idnr[i + 1] == c && idnr[i + 2] == c)
                    return false;
            }
        }
        return PassesMod1110(idnr);
    }

    /// <summary>German tax number (Steuernummer): 10 or 11 digits as printed, or the 13-digit ELSTER format.</summary>
    /// <param name="value">The string to check. Spaces, dashes and slashes allowed, e.g. <c>98/815/08152</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid tax number.</returns>
    public static bool IsValidDeStnr(string value)
    {
        if (!TryCompact(value, SpacesDashesAndSlashes, allowLetters: false, out var stnr))
            return false;
        if (stnr.Length is 10 or 11)
            return true;
        if (stnr.Length != 13 || stnr[4] != '0' || !DeStnrLandRegex.IsMatch(stnr))
            return false;
        bool nrw = stnr[0] == '5';
        var bezirk = nrw ? stnr.Substring(5, 4) : stnr.Substring(5, 3);
        if (bezirk is "000" or "998" or "999" or "0000" or "0998" or "0999")
            return false;
        if (DeStnrBezirkFrom100Regex.IsMatch(stnr) && int.Parse(bezirk, System.Globalization.CultureInfo.InvariantCulture) < 100)
            return false;
        if (nrw && int.Parse(stnr.AsSpan(9, 4), System.Globalization.CultureInfo.InvariantCulture) <= 9)
            return false;
        return !(stnr[0] == '9' && string.Equals(stnr[5..], "99999999", StringComparison.Ordinal));
    }

    /// <summary>German commercial register number: HRA, HRB, GnR, GsR, PR or VR, 1–6 digits and an optional 1–3 letter suffix.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed, e.g. <c>HRB 12345 B</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid register number.</returns>
    public static bool IsValidDeTradeRegister(string value)
    {
        return TryCompact(value, Spaces, allowLetters: true, out var number)
            && DeTradeRegisterRegex.IsMatch(number);
    }

    /// <summary>Leitweg-ID for XRechnung: Grobadressierung, optional Feinadressierung and ISO 7064 mod 97-10 check digits, separated by hyphens.</summary>
    /// <param name="value">The string to check. Any case; hyphens are part of the format and spaces are not allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid Leitweg-ID.</returns>
    public static bool IsValidDeLeitweg(string value)
    {
        if (value is null)
            return false;
        var text = Trim(value);
        var upper = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
            upper.Append(ToUpperAscii(c));
        var match = DeLeitwegRegex.Match(upper.ToString());
        if (!match.Success)
            return false;
        var grob = match.Groups[1].Value;
        if (Array.IndexOf(DeLeitwegGrobLengths, grob.Length) < 0)
            return false;
        int land = ((grob[0] - '0') * 10) + (grob[1] - '0');
        if (!(land is >= 1 and <= 16 || land == 99))
            return false;
        return Mod97(grob + match.Groups[2].Value + match.Groups[3].Value) == 1;
    }

    /// <summary>German pension insurance number (Rentenversicherungsnummer): 12 characters with a weighted check digit.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed, e.g. <c>15 070649 C 103</c>.</param>
    /// <returns><see langword="true"/> if the value is a valid pension insurance number.</returns>
    public static bool IsValidDeRvnr(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var rvnr) || rvnr.Length != 12
            || !IsAllDigits(rvnr[..8]) || !IsAsciiLetter(rvnr[8]) || !IsAllDigits(rvnr[9..]))
            return false;
        // The letter becomes its two-digit alphabet position (A=01 ... Z=26).
        int letter = rvnr[8] - 'A' + 1;
        var digits = string.Concat(rvnr.AsSpan(0, 8), letter.ToString("D2", System.Globalization.CultureInfo.InvariantCulture), rvnr.AsSpan(9, 2));
        int[] weights = [2, 1, 2, 5, 7, 1, 2, 1, 2, 1, 2, 1];
        return DigitSumCheck(digits, weights) == rvnr[11] - '0';
    }

    /// <summary>German health insurance number (Krankenversichertennummer): a letter, 8 digits and a check digit.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid health insurance number.</returns>
    public static bool IsValidDeKvnr(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var kvnr) || kvnr.Length != 10
            || !IsAsciiLetter(kvnr[0]) || !IsAllDigits(kvnr[1..]))
            return false;
        int letter = kvnr[0] - 'A' + 1;
        var digits = string.Concat(letter.ToString("D2", System.Globalization.CultureInfo.InvariantCulture), kvnr.AsSpan(1, 8));
        int[] weights = [1, 2, 1, 2, 1, 2, 1, 2, 1, 2];
        return DigitSumCheck(digits, weights) == kvnr[9] - '0';
    }

    /// <summary>German identity card number: 9 characters from the document alphabet and an optional ICAO 9303 check digit.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid identity card number.</returns>
    public static bool IsValidDeIdCard(string value) => IsValidDeDocumentNumber(value);

    /// <summary>German passport number: 9 characters from the document alphabet and an optional ICAO 9303 check digit.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid passport number.</returns>
    public static bool IsValidDePassport(string value) => IsValidDeDocumentNumber(value);

    /// <summary>German securities identification number (WKN): 6 letters and digits, without I and O.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid WKN.</returns>
    public static bool IsValidDeWkn(string value)
    {
        return TryCompact(value, Spaces, allowLetters: true, out var wkn) && DeWknRegex.IsMatch(wkn);
    }

    /// <summary>German vehicle registration plate: district (1–3 letters), 1–2 letters, 1–4 digits and an optional E or H.</summary>
    /// <param name="value">The string to check. Any case; the district may contain Ä, Ö and Ü; a hyphen or space marks the end of the district.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid German plate.</returns>
    public static bool IsValidDePlate(string value)
    {
        if (value is null)
            return false;
        var text = Trim(value);
        var upper = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            upper.Append(c switch
            {
                'ä' => 'Ä',
                'ö' => 'Ö',
                'ü' => 'Ü',
                _ => ToUpperAscii(c),
            });
        }
        var match = DePlateRegex.Match(upper.ToString());
        if (!match.Success)
            return false;

        var letters = match.Groups[1].Value;
        var groups = letters.Split([' ', ' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        bool validSplit = false;
        if (groups.Length == 1)
        {
            var run = groups[0];
            for (int k = 1; k <= 3 && k < run.Length; k++)
            {
                if (IsDePlateSplit(run[..k], run[k..]))
                    validSplit = true;
            }
        }
        else if (groups.Length == 2)
        {
            validSplit = IsDePlateSplit(groups[0], groups[1]);
        }
        if (!validSplit)
            return false;

        int count = groups[0].Length + (groups.Length == 2 ? groups[1].Length : 0) + match.Groups[2].Value.Length;
        return match.Groups[3].Success ? count <= 7 : count <= 8;
    }

    // District 1-3 letters (umlauts allowed), Erkennung 1-2 letters without umlauts.
    private static bool IsDePlateSplit(string district, string erkennung)
    {
        if (district.Length is < 1 or > 3 || erkennung.Length is < 1 or > 2)
            return false;
        foreach (char c in erkennung)
        {
            if (c is < 'A' or > 'Z')
                return false;
        }
        return true;
    }

    private static bool IsValidDeDocumentNumber(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var number) || !DeDocumentRegex.IsMatch(number))
            return false;
        if (number.Length == 9)
            return true;
        // ICAO 9303: weights 7, 3, 1 repeating; letters count as 10..35.
        int[] weights = [7, 3, 1];
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            char c = number[i];
            int charValue = IsAsciiDigit(c) ? c - '0' : c - 'A' + 10;
            sum += charValue * weights[i % 3];
        }
        return sum % 10 == number[9] - '0';
    }

    // ISO 7064 MOD 11,10 over a digit string whose last digit is the check digit.
    private static bool PassesMod1110(string digits)
    {
        int product = 10;
        for (int i = 0; i < digits.Length - 1; i++)
        {
            int sum = (digits[i] - '0' + product) % 10;
            if (sum == 0)
                sum = 10;
            product = sum * 2 % 11;
        }
        int check = 11 - product;
        if (check == 10)
            check = 0;
        return check == digits[^1] - '0';
    }

    // Sum of the digit sums of digit x weight products, mod 10.
    private static int DigitSumCheck(string digits, int[] weights)
    {
        int sum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            int product = (digits[i] - '0') * weights[i];
            sum += (product / 10) + (product % 10);
        }
        return sum % 10;
    }
}
