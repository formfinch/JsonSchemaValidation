// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

namespace FormFinch.JsonSchemaValidation.Formats;

internal static partial class ExtendedFormatValidators
{
    // Firmenbuchnummer check letters, indexed by the number mod 17.
    private const string AtFnCheckLetters = "ABDFGHIKMPSTVWXYZ";

    private static readonly Regex AtFnRegex = ShapeRegex("^[1-9][0-9]{0,5}[A-Z]$");
    private static readonly Regex AtPassportRegex = ShapeRegex("^[A-Z]{1,2}[0-9]{7}$");

    // Normal plate (district, number, letters) or Wunschkennzeichen / authority plate (letters, number). No Q.
    private static readonly Regex AtPlateRegex = ShapeRegex(
        "^(?:[A-PR-Z]{1,2}[1-9][0-9]{0,4}[A-PR-Z]{1,3}|[A-PR-Z]{1,7}[1-9][0-9]{0,4})$");

    private static readonly Regex LiPlateRegex = ShapeRegex("^FL[1-9][0-9]{0,4}$");

    /// <summary>Austrian phone number: 5–13 significant digits after the <c>0</c>, <c>+43</c> or <c>0043</c> prefix.</summary>
    /// <param name="value">The string to check. Spaces, dashes, dots, slashes and parentheses allowed, e.g. <c>0664/123 45 67</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Austrian phone number.</returns>
    public static bool IsValidAtPhone(string value)
    {
        return TryParsePhone(value, "43", out var national)
            && national.Length is >= 5 and <= 13;
    }

    /// <summary>Austrian postcode: 4 digits, the first not 0.</summary>
    /// <param name="value">The string to check. No separators.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Austrian postcode.</returns>
    public static bool IsValidAtPostcode(string value) => IsFourDigitPostcode(value);

    /// <summary>Austrian VAT identification number (UID): <c>ATU</c> + 8 digits with a check digit.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid Austrian VAT identification number.</returns>
    public static bool IsValidAtVat(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var vat) || vat.Length != 11
            || !vat.StartsWith("ATU", StringComparison.Ordinal) || !IsAllDigits(vat[3..]))
            return false;
        // Digits 1, 3, 5, 7 count as-is; 2, 4, 6 are doubled with 9 subtracted above 9.
        int sum = 0;
        for (int i = 0; i < 7; i++)
            sum += i % 2 == 0 ? vat[3 + i] - '0' : DoubleDigit(vat[3 + i] - '0');
        return (10 - ((sum + 4) % 10)) % 10 == vat[10] - '0';
    }

    /// <summary>Austrian social security number: 10 digits with a check digit in position 4.</summary>
    /// <param name="value">The string to check. Spaces allowed, e.g. <c>1237 010180</c>.</param>
    /// <returns><see langword="true"/> if the value is a valid Austrian social security number.</returns>
    public static bool IsValidAtSvnr(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: false, out var svnr) || svnr.Length != 10 || svnr[0] == '0')
            return false;
        int[] weights = [3, 7, 9, 0, 5, 8, 4, 2, 1, 6];
        int sum = 0;
        for (int i = 0; i < 10; i++)
            sum += weights[i] * (svnr[i] - '0');
        int check = sum % 11;
        // A remainder of 10 is never issued.
        return check < 10 && check == svnr[3] - '0';
    }

    /// <summary>Austrian company register number (Firmenbuchnummer): up to 6 digits and a mod 17 check letter.</summary>
    /// <param name="value">The string to check. Any case; optional <c>FN</c> prefix; spaces allowed, e.g. <c>FN 123456d</c>.</param>
    /// <returns><see langword="true"/> if the value is a valid Firmenbuchnummer.</returns>
    public static bool IsValidAtFn(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var fn))
            return false;
        if (fn.StartsWith("FN", StringComparison.Ordinal))
            fn = fn[2..];
        if (!AtFnRegex.IsMatch(fn))
            return false;
        int number = int.Parse(fn.AsSpan(0, fn.Length - 1), System.Globalization.CultureInfo.InvariantCulture);
        return AtFnCheckLetters[number % 17] == fn[^1];
    }

    /// <summary>Austrian tax number (Abgabenkontonummer): 9 digits with a check digit.</summary>
    /// <param name="value">The string to check. Spaces, dashes and slashes allowed, e.g. <c>12-345/6782</c>.</param>
    /// <returns><see langword="true"/> if the value is a valid Austrian tax number.</returns>
    public static bool IsValidAtTin(string value)
    {
        if (!TryCompact(value, SpacesDashesAndSlashes, allowLetters: false, out var tin)
            || tin.Length != 9 || IsAllZeros(tin))
            return false;
        int sum = 0;
        for (int i = 0; i < 8; i++)
            sum += i % 2 == 0 ? tin[i] - '0' : DoubleDigit(tin[i] - '0');
        return (10 - (sum % 10)) % 10 == tin[8] - '0';
    }

    /// <summary>Austrian licence plate: district and number with 1–3 letters, or letters and number (Wunschkennzeichen).</summary>
    /// <param name="value">The string to check. Any case; spaces and dashes allowed, e.g. <c>W 12345 A</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Austrian licence plate.</returns>
    public static bool IsValidAtPlate(string value)
    {
        return TryCompact(value, SpacesAndDashes, allowLetters: true, out var plate)
            && plate.Length <= 8
            && AtPlateRegex.IsMatch(plate);
    }

    /// <summary>Austrian passport number: 1 or 2 letters and 7 digits.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Austrian passport number.</returns>
    public static bool IsValidAtPassport(string value)
    {
        return TryCompact(value, Spaces, allowLetters: true, out var passport)
            && AtPassportRegex.IsMatch(passport);
    }

    /// <summary>Liechtenstein phone number: 7 digits (first 2, 3, 4, 7, 8, 9) or 9 digits (first 5 or 6); no trunk prefix.</summary>
    /// <param name="value">The string to check. Spaces, dashes, dots, slashes and parentheses allowed, e.g. <c>+423 234 56 78</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Liechtenstein phone number.</returns>
    public static bool IsValidLiPhone(string value)
    {
        if (!TryParsePhone(value, "423", out var national, trunkPrefix: false))
            return false;
        return national.Length switch
        {
            7 => national[0] is '2' or '3' or '4' or '7' or '8' or '9',
            9 => national[0] is '5' or '6',
            _ => false,
        };
    }

    /// <summary>Liechtenstein postcode: 9485–9498.</summary>
    /// <param name="value">The string to check. No separators.</param>
    /// <returns><see langword="true"/> if the value is a Liechtenstein postcode.</returns>
    public static bool IsValidLiPostcode(string value)
    {
        if (!IsFourDigitPostcode(value))
            return false;
        int code = int.Parse(Trim(value), System.Globalization.CultureInfo.InvariantCulture);
        return code is >= 9485 and <= 9498;
    }

    /// <summary>Liechtenstein personal identification number (PEID): up to 12 digits, at least 4 without leading zeros.</summary>
    /// <param name="value">The string to check. Spaces and dots allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid PEID.</returns>
    public static bool IsValidLiPeid(string value)
    {
        if (!TryCompact(value, SpacesAndDots, allowLetters: false, out var peid) || peid.Length > 12)
            return false;
        return peid.TrimStart('0').Length >= 4;
    }

    /// <summary>Liechtenstein licence plate: <c>FL</c> and 1–5 digits.</summary>
    /// <param name="value">The string to check. Any case; spaces and dashes allowed, e.g. <c>FL 12345</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Liechtenstein licence plate.</returns>
    public static bool IsValidLiPlate(string value)
    {
        return TryCompact(value, SpacesAndDashes, allowLetters: true, out var plate)
            && LiPlateRegex.IsMatch(plate);
    }

    // Four digits, the first not 0, with no separators (Austria, Belgium, Liechtenstein).
    private static bool IsFourDigitPostcode(string value)
    {
        if (value is null)
            return false;
        var text = Trim(value);
        return text.Length == 4 && text[0] is >= '1' and <= '9' && IsAllDigits(text);
    }

    // Luhn doubling: twice the digit, minus 9 when above 9.
    private static int DoubleDigit(int digit) => digit * 2 > 9 ? (digit * 2) - 9 : digit * 2;
}
