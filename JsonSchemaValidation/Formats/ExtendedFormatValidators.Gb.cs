// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

namespace FormFinch.JsonSchemaValidation.Formats;

internal static partial class ExtendedFormatValidators
{
    // Patterns run on compacted, upper-cased ASCII values only, so C# and JavaScript agree.
    private static readonly Regex GbPostcodeRegex = ShapeRegex(
        "^(?:[A-PR-UWYZ][0-9][0-9]?|[A-PR-UWYZ][A-HK-Y][0-9][0-9]?|[A-PR-UWYZ][0-9][ABCDEFGHJKPSTUW]|[A-PR-UWYZ][A-HK-Y][0-9][ABEHMNPRVWXY])[0-9][ABD-HJLNP-UW-Z]{2}$");

    private static readonly Regex GbNinoRegex = ShapeRegex(
        "^[A-CEGHJ-PR-TW-Z][A-CEGHJ-NPR-TW-Z][0-9]{6}[A-D]?$");

    private static readonly Regex GbPlateRegex = ShapeRegex(
        "^(?:[A-HJ-PR-Y]{2}[0-9]{2}[A-HJ-PR-Z]{3}|[A-Z][1-9][0-9]{0,2}[A-Z]{3}|[A-Z]{3}[1-9][0-9]{0,2}[A-Z]|[A-Z]{1,3}[1-9][0-9]{0,3}|[1-9][0-9]{0,3}[A-Z]{1,3})$");

    // Companies House company number prefixes (see FORMATS.md, gb-crn).
    private static readonly HashSet<string> GbCrnPrefixes = new(StringComparer.Ordinal)
    {
        "AC", "ZC", "FC", "GE", "LP", "OC", "SE", "SA", "SZ", "SF", "GS", "SL", "SO", "SC",
        "ES", "NA", "NZ", "NF", "GN", "NL", "NC", "R0", "NI", "EN", "IP", "SP", "IC", "SI",
        "NP", "NV", "RC", "SR", "NR", "NO", "BR", "CE", "CS", "OE", "PC", "SG",
    };

    private static readonly HashSet<string> GbNinoExcludedPrefixes = new(StringComparer.Ordinal)
    {
        "BG", "GB", "KN", "NK", "NT", "TN", "ZZ",
    };

    // DfE UPN check alphabet: A-Z without I, O and S.
    private const string GbUpnAlphabet = "ABCDEFGHJKLMNPQRTUVWXYZ";

    /// <summary>UK phone number: 10 significant digits (or 9 in some 01 areas and legacy 0800) after the <c>0</c>, <c>+44</c> or <c>0044</c> prefix.</summary>
    /// <param name="value">The string to check. Spaces, dashes, dots, slashes and parentheses allowed, e.g. <c>+44 (0)20 7946 0123</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid UK phone number.</returns>
    public static bool IsValidGbPhone(string value)
    {
        if (!TryParsePhone(value, "44", out var national))
            return false;
        if (national.Length == 10)
            return national[0] is '1' or '2' or '3' or '5' or '7' or '8' or '9';
        if (national.Length == 9)
        {
            // 9-digit numbers exist only in the mixed 01xxx(x) areas (not 011x or 01x1) and legacy 0800.
            return national[0] == '1'
                ? national[1] != '1' && national[2] != '1'
                : national.StartsWith("800", StringComparison.Ordinal);
        }
        return false;
    }

    /// <summary>UK postcode: outward code (A9, A99, AA9, AA99, A9A, AA9A) and inward code (9AA), or <c>GIR 0AA</c>.</summary>
    /// <param name="value">The string to check. Any case; at most one space, before the inward code.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid UK postcode.</returns>
    public static bool IsValidGbPostcode(string value)
    {
        if (value is null)
            return false;
        var text = Trim(value);
        if (text.Length >= 6 && IsSpace(text[^4]))
            text = string.Concat(text.AsSpan(0, text.Length - 4), text.AsSpan(text.Length - 3));
        if (!TryCompact(text, string.Empty, allowLetters: true, out var postcode))
            return false;
        return string.Equals(postcode, "GIR0AA", StringComparison.Ordinal) || GbPostcodeRegex.IsMatch(postcode);
    }

    /// <summary>NHS number: 10 digits with a modulus 11 check digit.</summary>
    /// <param name="value">The string to check. Spaces and dashes allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid NHS number.</returns>
    public static bool IsValidGbNhs(string value)
    {
        if (!TryCompact(value, SpacesAndDashes, allowLetters: false, out var digits)
            || digits.Length != 10 || IsAllZeros(digits))
            return false;
        // Weights 10..2 on the first nine digits and 1 on the check digit. A check value of 10 is
        // never issued; no single digit satisfies it, so this form rejects it.
        int sum = 0;
        for (int i = 0; i < 10; i++)
            sum += (10 - i) * (digits[i] - '0');
        return sum % 11 == 0;
    }

    /// <summary>UK VAT registration number: 9 digits (mod 97 or the 9755 series), 12 digits (branch), or GD/HA numbers.</summary>
    /// <param name="value">The string to check. Any case; optional <c>GB</c> or <c>XI</c> prefix; spaces, dots and dashes allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid UK VAT registration number.</returns>
    public static bool IsValidGbVat(string value)
    {
        if (!TryCompact(value, SpacesDotsAndDashes, allowLetters: true, out var vat))
            return false;
        if (vat.StartsWith("GB", StringComparison.Ordinal) || vat.StartsWith("XI", StringComparison.Ordinal))
            vat = vat[2..];

        if (vat.Length == 5 && (vat.StartsWith("GD", StringComparison.Ordinal) || vat.StartsWith("HA", StringComparison.Ordinal)))
        {
            if (!IsAsciiDigit(vat[2]) || !IsAsciiDigit(vat[3]) || !IsAsciiDigit(vat[4]))
                return false;
            int number = ((vat[2] - '0') * 100) + ((vat[3] - '0') * 10) + (vat[4] - '0');
            // Government departments GD000-GD499, health authorities HA500-HA999.
            return vat[0] == 'G' ? number < 500 : number >= 500;
        }

        if ((vat.Length != 9 && vat.Length != 12) || !IsAllDigits(vat))
            return false;
        if (IsAllZeros(vat[..7]))
            return false;
        int[] weights = [8, 7, 6, 5, 4, 3, 2, 10, 1];
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += weights[i] * (vat[i] - '0');
        int remainder = sum % 97;
        // Classic mod 97, or the 9755 series (55 added before mod 97) for numbers from 100 upwards.
        return remainder == 0 || (remainder == 42 && vat[0] != '0');
    }

    /// <summary>Companies House company registration number: 8 digits, or a known two-character prefix and 6 digits.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid company number.</returns>
    public static bool IsValidGbCrn(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var crn) || crn.Length != 8)
            return false;
        if (IsAllDigits(crn))
            return !IsAllZeros(crn);
        var number = crn[2..];
        return GbCrnPrefixes.Contains(crn[..2]) && IsAllDigits(number) && !IsAllZeros(number);
    }

    /// <summary>UK bank sort code: 6 digits, not all zeros.</summary>
    /// <param name="value">The string to check. Spaces and dashes allowed, e.g. <c>12-34-56</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid sort code.</returns>
    public static bool IsValidGbSortCode(string value)
    {
        return TryCompact(value, SpacesAndDashes, allowLetters: false, out var digits)
            && digits.Length == 6 && !IsAllZeros(digits);
    }

    /// <summary>UK bank account number: 8 digits, not all zeros.</summary>
    /// <param name="value">The string to check. Spaces and dashes allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid account number.</returns>
    public static bool IsValidGbAccountNumber(string value)
    {
        return TryCompact(value, SpacesAndDashes, allowLetters: false, out var digits)
            && digits.Length == 8 && !IsAllZeros(digits);
    }

    /// <summary>National Insurance number: two prefix letters, 6 digits and an optional suffix A–D.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed, e.g. <c>AB 12 34 56 C</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid National Insurance number.</returns>
    public static bool IsValidGbNino(string value)
    {
        return TryCompact(value, Spaces, allowLetters: true, out var nino)
            && GbNinoRegex.IsMatch(nino)
            && !GbNinoExcludedPrefixes.Contains(nino[..2]);
    }

    /// <summary>Unique Pupil Number: check letter, 11 digits and a digit or letter, with a mod 23 check letter.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid UPN.</returns>
    public static bool IsValidGbUpn(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var upn) || upn.Length != 13)
            return false;
        if (GbUpnAlphabet.IndexOf(upn[0], StringComparison.Ordinal) < 0 || !IsAllDigits(upn[1..12]))
            return false;
        char last = upn[12];
        if (!IsAsciiDigit(last) && GbUpnAlphabet.IndexOf(last, StringComparison.Ordinal) < 0)
            return false;
        int sum = 0;
        for (int i = 1; i < 13; i++)
        {
            char c = upn[i];
            int charValue = IsAsciiDigit(c) ? c - '0' : GbUpnAlphabet.IndexOf(c, StringComparison.Ordinal);
            sum += (i + 1) * charValue;
        }
        return GbUpnAlphabet[sum % 23] == upn[0];
    }

    /// <summary>UK vehicle registration mark: current, prefix, suffix and dateless formats, at most 7 characters.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed, e.g. <c>AB12 CDE</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid UK registration mark.</returns>
    public static bool IsValidGbPlate(string value)
    {
        return TryCompact(value, Spaces, allowLetters: true, out var plate)
            && plate.Length <= 7
            && GbPlateRegex.IsMatch(plate);
    }
}
