// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

namespace FormFinch.JsonSchemaValidation.Formats;

internal static partial class ExtendedFormatValidators
{
    private static readonly Regex ChPassportRegex = ShapeRegex("^[A-HJ-NP-Z][0-9A-HJ-NP-Z]{7}$");
    private static readonly Regex LuPlateRegex = ShapeRegex("^(?:[A-HJ-NP-Z]{2}[0-9]{4}|[A-HJ-NP-Z]{2}[0-9]{2}|[0-9]{4,5})$");
    private static readonly Regex LuRcsRegex = ShapeRegex("^[A-Z][1-9][0-9]{0,5}$");

    // ISO 3166-2:CH canton codes, as used on licence plates.
    private static readonly HashSet<string> ChCantons = new(StringComparer.Ordinal)
    {
        "AG", "AI", "AR", "BE", "BL", "BS", "FR", "GE", "GL", "GR", "JU", "LU", "NE", "NW",
        "OW", "SG", "SH", "SO", "SZ", "TG", "TI", "UR", "VD", "VS", "ZG", "ZH",
    };

    private static readonly string[] ChVatSuffixes = ["", "MWST", "TVA", "IVA"];

    // Recursive mod 10 carry table (Swiss QR reference, formerly ESR).
    private static readonly int[] ChQrReferenceTable = [0, 9, 4, 6, 8, 2, 7, 1, 3, 5];

    // Verhoeff dihedral group, permutation and (implicit) inverse tables.
    private static readonly int[,] VerhoeffD =
    {
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, { 1, 2, 3, 4, 0, 6, 7, 8, 9, 5 },
        { 2, 3, 4, 0, 1, 7, 8, 9, 5, 6 }, { 3, 4, 0, 1, 2, 8, 9, 5, 6, 7 },
        { 4, 0, 1, 2, 3, 9, 5, 6, 7, 8 }, { 5, 9, 8, 7, 6, 0, 4, 3, 2, 1 },
        { 6, 5, 9, 8, 7, 1, 0, 4, 3, 2 }, { 7, 6, 5, 9, 8, 2, 1, 0, 4, 3 },
        { 8, 7, 6, 5, 9, 3, 2, 1, 0, 4 }, { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 },
    };

    private static readonly int[,] VerhoeffP =
    {
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, { 1, 5, 7, 6, 2, 8, 3, 0, 9, 4 },
        { 5, 8, 0, 3, 7, 9, 6, 1, 4, 2 }, { 8, 9, 1, 6, 0, 4, 3, 5, 2, 7 },
        { 9, 4, 5, 3, 1, 2, 6, 8, 7, 0 }, { 4, 2, 8, 6, 5, 7, 3, 9, 0, 1 },
        { 2, 7, 9, 3, 8, 0, 6, 4, 1, 5 }, { 7, 0, 4, 6, 9, 1, 3, 2, 5, 8 },
    };

    /// <summary>Swiss phone number: 9 significant digits, the first 2–9, after the <c>0</c>, <c>+41</c> or <c>0041</c> prefix.</summary>
    /// <param name="value">The string to check. Spaces, dashes, dots, slashes and parentheses allowed, e.g. <c>+41 (0)78 123 45 67</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Swiss phone number.</returns>
    public static bool IsValidChPhone(string value)
    {
        return TryParsePhone(value, "41", out var national)
            && national.Length == 9 && national[0] >= '2';
    }

    /// <summary>Swiss postcode: 4 digits, the first not 0, with an optional <c>CH-</c> prefix.</summary>
    /// <param name="value">The string to check. Any case prefix; no other separators.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Swiss postcode.</returns>
    public static bool IsValidChPostcode(string value) => IsPrefixedFourDigitPostcode(value, "CH-");

    /// <summary>Swiss enterprise identification number (UID): <c>CHE</c> + 9 digits with a mod 11 check digit.</summary>
    /// <param name="value">The string to check. Any case; spaces, dots and dashes allowed, e.g. <c>CHE-123.456.789</c>.</param>
    /// <returns><see langword="true"/> if the value is a valid UID.</returns>
    public static bool IsValidChUid(string value)
    {
        return TryCompact(value, SpacesDotsAndDashes, allowLetters: true, out var uid)
            && uid.Length == 12
            && uid.StartsWith("CHE", StringComparison.Ordinal)
            && PassesChUidCheck(uid[3..]);
    }

    /// <summary>Swiss VAT number: a UID, optionally followed by <c>MWST</c>, <c>TVA</c> or <c>IVA</c>.</summary>
    /// <param name="value">The string to check. Any case; spaces, dots and dashes allowed, e.g. <c>CHE-123.456.789 MWST</c>.</param>
    /// <returns><see langword="true"/> if the value is a valid Swiss VAT number.</returns>
    public static bool IsValidChVat(string value)
    {
        if (!TryCompact(value, SpacesDotsAndDashes, allowLetters: true, out var vat)
            || vat.Length < 12 || !vat.StartsWith("CHE", StringComparison.Ordinal))
            return false;
        return Array.IndexOf(ChVatSuffixes, vat[12..]) >= 0 && PassesChUidCheck(vat[3..12]);
    }

    /// <summary>Swiss social security number (AHV/AVS): 13 digits starting with 756 and an EAN-13 check digit.</summary>
    /// <param name="value">The string to check. Spaces and dots allowed, e.g. <c>756.1234.5678.97</c>.</param>
    /// <returns><see langword="true"/> if the value is a valid AHV number.</returns>
    public static bool IsValidChAhv(string value)
    {
        if (!TryCompact(value, SpacesAndDots, allowLetters: false, out var ahv) || ahv.Length != 13
            || !ahv.StartsWith("756", StringComparison.Ordinal))
            return false;
        int sum = 0;
        for (int i = 0; i < 13; i++)
            sum += (ahv[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return sum % 10 == 0;
    }

    /// <summary>Swiss QR reference: 27 digits, not all zeros, with a recursive mod 10 check digit.</summary>
    /// <param name="value">The string to check. Spaces allowed, e.g. <c>21 00000 00003 13947 14300 09017</c>.</param>
    /// <returns><see langword="true"/> if the value is a valid QR reference.</returns>
    public static bool IsValidChQrReference(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: false, out var reference)
            || reference.Length != 27 || IsAllZeros(reference))
            return false;
        int carry = 0;
        for (int i = 0; i < 26; i++)
            carry = ChQrReferenceTable[(carry + reference[i] - '0') % 10];
        return (10 - carry) % 10 == reference[26] - '0';
    }

    /// <summary>Swiss QR-IBAN: a valid CH or LI IBAN whose institution identification is 30000–31999.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid QR-IBAN.</returns>
    public static bool IsValidChQrIban(string value)
    {
        if (!IsValidIban(value) || !TryCompact(value, Spaces, allowLetters: true, out var iban))
            return false;
        if (!iban.StartsWith("CH", StringComparison.Ordinal) && !iban.StartsWith("LI", StringComparison.Ordinal))
            return false;
        var iid = iban.Substring(4, 5);
        return IsAllDigits(iid) && string.CompareOrdinal(iid, "30000") >= 0 && string.CompareOrdinal(iid, "31999") <= 0;
    }

    /// <summary>Swiss licence plate: a canton code and 1–6 digits.</summary>
    /// <param name="value">The string to check. Any case; spaces, dashes and dots allowed, e.g. <c>ZH 123456</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Swiss licence plate.</returns>
    public static bool IsValidChPlate(string value)
    {
        if (!TryCompact(value, SpacesDotsAndDashes, allowLetters: true, out var plate) || plate.Length is < 3 or > 8)
            return false;
        var number = plate[2..];
        return ChCantons.Contains(plate[..2]) && IsAllDigits(number) && number[0] != '0';
    }

    /// <summary>Swiss passport or identity card number: 8 letters and digits starting with a letter, without I and O.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a plausible Swiss document number.</returns>
    public static bool IsValidChPassport(string value)
    {
        return TryCompact(value, Spaces, allowLetters: true, out var number)
            && ChPassportRegex.IsMatch(number);
    }

    /// <summary>Luxembourg phone number: mobile (6…) 9 digits, otherwise 4–11 digits, first 2–9; no trunk prefix.</summary>
    /// <param name="value">The string to check. Spaces, dashes, dots, slashes and parentheses allowed, e.g. <c>+352 621 123 456</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Luxembourg phone number.</returns>
    public static bool IsValidLuPhone(string value)
    {
        if (!TryParsePhone(value, "352", out var national, trunkPrefix: false) || national[0] < '2')
            return false;
        return national[0] == '6' ? national.Length == 9 : national.Length is >= 4 and <= 11;
    }

    /// <summary>Luxembourg postcode: 4 digits, the first not 0, with an optional <c>L-</c> prefix.</summary>
    /// <param name="value">The string to check. Any case prefix; no other separators.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Luxembourg postcode.</returns>
    public static bool IsValidLuPostcode(string value) => IsPrefixedFourDigitPostcode(value, "L-");

    /// <summary>Luxembourg VAT number: <c>LU</c> + 8 digits, the last two being the first six mod 89.</summary>
    /// <param name="value">The string to check. Any case; spaces, dots and dashes allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid Luxembourg VAT number.</returns>
    public static bool IsValidLuVat(string value)
    {
        if (!TryCompact(value, SpacesDotsAndDashes, allowLetters: true, out var vat) || vat.Length != 10
            || !vat.StartsWith("LU", StringComparison.Ordinal) || !IsAllDigits(vat[2..]) || IsAllZeros(vat[2..]))
            return false;
        int body = int.Parse(vat.AsSpan(2, 6), System.Globalization.CultureInfo.InvariantCulture);
        int check = int.Parse(vat.AsSpan(8, 2), System.Globalization.CultureInfo.InvariantCulture);
        return body % 89 == check;
    }

    /// <summary>Luxembourg national identification number (matricule): 13 digits with a Luhn and a Verhoeff check digit.</summary>
    /// <param name="value">The string to check. Spaces, dots and dashes allowed, e.g. <c>1900 0101 001 52</c>.</param>
    /// <returns><see langword="true"/> if the value is a valid matricule.</returns>
    public static bool IsValidLuMatricule(string value)
    {
        if (!TryCompact(value, SpacesDotsAndDashes, allowLetters: false, out var matricule) || matricule.Length != 13)
            return false;
        // Both check digits are computed over the same first 11 digits.
        var body = matricule[..11];
        return PassesLuhn(body + matricule[11]) && PassesVerhoeff(body + matricule[12]);
    }

    /// <summary>Luxembourg trade register (RCS) number: a section letter and 1–6 digits.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed, e.g. <c>B123456</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid RCS number.</returns>
    public static bool IsValidLuRcs(string value)
    {
        return TryCompact(value, Spaces, allowLetters: true, out var rcs) && LuRcsRegex.IsMatch(rcs);
    }

    /// <summary>Luxembourg licence plate: 2 letters and 4 or 2 digits, or 4–5 digits.</summary>
    /// <param name="value">The string to check. Any case; spaces and dashes allowed, e.g. <c>AB 1234</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Luxembourg licence plate.</returns>
    public static bool IsValidLuPlate(string value)
    {
        return TryCompact(value, SpacesAndDashes, allowLetters: true, out var plate) && LuPlateRegex.IsMatch(plate);
    }

    // UID digits: weights 5 4 3 2 7 6 5 4, check (11 - sum mod 11) mod 11; 10 is never issued.
    private static bool PassesChUidCheck(string digits)
    {
        if (digits.Length != 9 || !IsAllDigits(digits) || IsAllZeros(digits))
            return false;
        int[] weights = [5, 4, 3, 2, 7, 6, 5, 4];
        int sum = 0;
        for (int i = 0; i < 8; i++)
            sum += weights[i] * (digits[i] - '0');
        int check = (11 - (sum % 11)) % 11;
        return check != 10 && check == digits[8] - '0';
    }

    // Four digits, the first not 0, optionally preceded by a country prefix such as "CH-" (any case).
    private static bool IsPrefixedFourDigitPostcode(string value, string prefix)
    {
        if (value is null)
            return false;
        var text = Trim(value);
        if (text.Length == prefix.Length + 4)
        {
            for (int i = 0; i < prefix.Length; i++)
            {
                if (ToUpperAscii(text[i]) != prefix[i])
                    return false;
            }
            text = text[prefix.Length..];
        }
        return IsFourDigitPostcode(text);
    }

    private static bool PassesLuhn(string digits)
    {
        int sum = 0;
        for (int p = 0; p < digits.Length; p++)
        {
            int d = digits[digits.Length - 1 - p] - '0';
            sum += p % 2 == 1 ? DoubleDigit(d) : d;
        }
        return sum % 10 == 0;
    }

    private static bool PassesVerhoeff(string digits)
    {
        int c = 0;
        for (int p = 0; p < digits.Length; p++)
            c = VerhoeffD[c, VerhoeffP[p % 8, digits[digits.Length - 1 - p] - '0']];
        return c == 0;
    }
}
