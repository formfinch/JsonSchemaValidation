// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Collections.ObjectModel;
using System.Text;

namespace FormFinch.JsonSchemaValidation.Formats;

/// <summary>
/// Checks for the opt-in extended format catalog (IBAN, BIC, ISBN-13 and national formats).
/// </summary>
/// <remarks>
/// <para>
/// Each check accepts the common ways people write a value (spaces, dashes, dots, national or
/// international prefixes, any case) as documented in <c>FORMATS.md</c>. A check never
/// changes the value; it only decides whether the value is acceptable.
/// </para>
/// <para>
/// Schema validation uses these checks only when <see cref="SchemaValidationOptions.EnableExtendedFormats"/>
/// is set and format assertion is active for the schema's draft.
/// </para>
/// </remarks>
internal static class ExtendedFormatValidators
{
    private static readonly Dictionary<string, Func<string, bool>> Checks = new(StringComparer.Ordinal)
    {
        ["iso-13616-iban"] = IsValidIban,
        ["iso-9362-bic"] = IsValidBic,
        ["iso-2108-isbn"] = IsValidIsbn13,
        ["nl-bsn"] = IsValidNlBsn,
        ["nl-vat"] = IsValidNlVat,
        ["nl-kvk"] = IsValidNlKvk,
        ["nl-postcode"] = IsValidNlPostcode,
        ["nl-phone"] = IsValidNlPhone,
        ["be-phone"] = IsValidBePhone,
    };

    // Separator sets. A space is U+0020 or a no-break space (U+00A0), which is common in pasted text.
    private const string Spaces = " \u00A0";
    private const string SpacesAndDashes = " \u00A0-";
    private const string SpacesAndDots = " \u00A0.";
    private const string SpacesDotsAndDashes = " \u00A0.-";

    private static readonly char[] TrimChars = [' ', '\u00A0', '\t', '\r', '\n'];

    private static readonly ReadOnlyCollection<string> Names = Checks.Keys.ToList().AsReadOnly();

    // IBAN length per country, from the SWIFT IBAN Registry.
    private static readonly Dictionary<string, int> IbanLengths = new(StringComparer.Ordinal)
    {
        ["AD"] = 24, ["AE"] = 23, ["AL"] = 28, ["AT"] = 20, ["AZ"] = 28, ["BA"] = 20, ["BE"] = 16,
        ["BG"] = 22, ["BH"] = 22, ["BI"] = 27, ["BR"] = 29, ["BY"] = 28, ["CH"] = 21, ["CR"] = 22,
        ["CY"] = 28, ["CZ"] = 24, ["DE"] = 22, ["DJ"] = 27, ["DK"] = 18, ["DO"] = 28, ["EE"] = 20,
        ["EG"] = 29, ["ES"] = 24, ["FI"] = 18, ["FK"] = 18, ["FO"] = 18, ["FR"] = 27, ["GB"] = 22,
        ["GE"] = 22, ["GI"] = 23, ["GL"] = 18, ["GR"] = 27, ["GT"] = 28, ["HN"] = 28, ["HR"] = 21,
        ["HU"] = 28, ["IE"] = 22, ["IL"] = 23, ["IQ"] = 23, ["IS"] = 26, ["IT"] = 27, ["JO"] = 30,
        ["KW"] = 30, ["KZ"] = 20, ["LB"] = 28, ["LC"] = 32, ["LI"] = 21, ["LT"] = 20, ["LU"] = 20,
        ["LV"] = 21, ["LY"] = 25, ["MC"] = 27, ["MD"] = 24, ["ME"] = 22, ["MK"] = 19, ["MN"] = 20,
        ["MR"] = 27, ["MT"] = 31, ["MU"] = 30, ["NI"] = 28, ["NL"] = 18, ["NO"] = 15, ["OM"] = 23,
        ["PK"] = 24, ["PL"] = 28, ["PS"] = 29, ["PT"] = 25, ["QA"] = 29, ["RO"] = 24, ["RS"] = 22,
        ["RU"] = 33, ["SA"] = 24, ["SC"] = 31, ["SD"] = 18, ["SE"] = 24, ["SI"] = 19, ["SK"] = 24,
        ["SM"] = 27, ["SO"] = 23, ["ST"] = 25, ["SV"] = 28, ["TL"] = 23, ["TN"] = 24, ["TR"] = 26,
        ["UA"] = 29, ["VA"] = 22, ["VG"] = 24, ["XK"] = 20, ["YE"] = 30,
    };

    /// <summary>Gets the names of all formats in the extended catalog.</summary>
    public static IReadOnlyCollection<string> FormatNames => Names;

    /// <summary>Returns whether <paramref name="format"/> is a format in the extended catalog.</summary>
    /// <param name="format">The format name, e.g. <c>"nl-phone"</c>.</param>
    /// <returns><see langword="true"/> if the catalog contains the format.</returns>
    public static bool IsKnownFormat(string format)
    {
        ArgumentNullException.ThrowIfNull(format);
        return Checks.ContainsKey(format);
    }

    /// <summary>Checks <paramref name="value"/> against a catalog format by name.</summary>
    /// <param name="format">The format name, e.g. <c>"nl-phone"</c>.</param>
    /// <param name="value">The string to check.</param>
    /// <returns><see langword="true"/> if the value is acceptable for the format.</returns>
    /// <exception cref="ArgumentException">The format is not in the catalog.</exception>
    public static bool IsValid(string format, string value)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (!Checks.TryGetValue(format, out var check))
        {
            throw new ArgumentException($"Unknown extended format '{format}'.", nameof(format));
        }
        return check(value);
    }

    internal static Dictionary<string, Func<string, bool>> Catalog => Checks;

    /// <summary>International Bank Account Number (ISO 13616): registry country length and mod-97 check.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed anywhere.</param>
    /// <returns><see langword="true"/> if the value is a valid IBAN.</returns>
    public static bool IsValidIban(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var iban))
            return false;
        if (iban.Length < 5 || !IsAsciiLetter(iban[0]) || !IsAsciiLetter(iban[1])
            || !IsAsciiDigit(iban[2]) || !IsAsciiDigit(iban[3]))
            return false;
        if (!IbanLengths.TryGetValue(iban[..2], out var length) || iban.Length != length)
            return false;
        return Mod97(string.Concat(iban.AsSpan(4), iban.AsSpan(0, 4))) == 1;
    }

    /// <summary>Business Identifier Code (ISO 9362): 8 or 11 characters.</summary>
    /// <param name="value">The string to check. Any case; spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid BIC.</returns>
    public static bool IsValidBic(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: true, out var bic))
            return false;
        if (bic.Length != 8 && bic.Length != 11)
            return false;
        for (int i = 0; i < 6; i++)
        {
            if (!IsAsciiLetter(bic[i]))
                return false;
        }
        return true;
    }

    /// <summary>ISBN-13: 978/979 prefix and mod-10 check digit.</summary>
    /// <param name="value">The string to check. Hyphens or spaces between digits; optional <c>ISBN</c> / <c>ISBN-13:</c> prefix.</param>
    /// <returns><see langword="true"/> if the value is a valid ISBN-13.</returns>
    public static bool IsValidIsbn13(string value)
    {
        if (value is null)
            return false;
        var text = Trim(value);
        if (text.StartsWith("ISBN", StringComparison.OrdinalIgnoreCase))
        {
            text = text[4..];
            if (text.StartsWith("-13", StringComparison.Ordinal))
                text = text[3..];
            if (text.StartsWith(':'))
                text = text[1..];
            text = Trim(text);
        }
        if (!TryCompact(text, SpacesAndDashes, allowLetters: false, out var isbn))
            return false;
        if (isbn.Length != 13 || !(isbn.StartsWith("978", StringComparison.Ordinal) || isbn.StartsWith("979", StringComparison.Ordinal)))
            return false;
        int sum = 0;
        for (int i = 0; i < 13; i++)
        {
            sum += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }
        return sum % 10 == 0;
    }

    /// <summary>Dutch citizen service number (BSN): 8 or 9 digits passing the elfproef.</summary>
    /// <param name="value">The string to check. Spaces, dots or dashes allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid BSN.</returns>
    public static bool IsValidNlBsn(string value)
    {
        if (!TryCompact(value, SpacesDotsAndDashes, allowLetters: false, out var digits))
            return false;
        if (digits.Length == 8)
            digits = "0" + digits;
        return digits.Length == 9 && PassesElfproef(digits);
    }

    /// <summary>Dutch VAT identification number: <c>NL</c> + 9 digits + <c>B</c> + 01–99, 11-check or mod-97.</summary>
    /// <param name="value">The string to check. Any case; spaces and dots allowed.</param>
    /// <returns><see langword="true"/> if the value is a valid Dutch VAT number.</returns>
    public static bool IsValidNlVat(string value)
    {
        if (!TryCompact(value, SpacesAndDots, allowLetters: true, out var vat))
            return false;
        if (vat.Length != 14 || vat[0] != 'N' || vat[1] != 'L' || vat[11] != 'B')
            return false;
        for (int i = 2; i < 14; i++)
        {
            if (i != 11 && !IsAsciiDigit(vat[i]))
                return false;
        }
        // The suffix after B runs from 01 to 99; 00 is never issued.
        if (vat[12] == '0' && vat[13] == '0')
            return false;
        // Numbers issued before 2020 carry an 11-check on the 9 digits; the btw-id issued to
        // sole proprietors since 2020 passes mod-97 over the whole identifier instead.
        return PassesElfproef(vat.Substring(2, 9)) || Mod97(vat) == 1;
    }

    /// <summary>Dutch Chamber of Commerce (KvK) number: 8 digits.</summary>
    /// <param name="value">The string to check. Spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid KvK number.</returns>
    public static bool IsValidNlKvk(string value)
    {
        if (!TryCompact(value, Spaces, allowLetters: false, out var digits))
            return false;
        return digits.Length == 8 && digits.AsSpan().ContainsAnyExcept('0');
    }

    /// <summary>Dutch postcode: 4 digits (not starting with 0) and 2 letters, not SA, SD or SS.</summary>
    /// <param name="value">The string to check. Any case; at most one space between digits and letters.</param>
    /// <returns><see langword="true"/> if the value is a valid Dutch postcode.</returns>
    public static bool IsValidNlPostcode(string value)
    {
        if (value is null)
            return false;
        var text = Trim(value);
        if (text.Length == 7 && IsSpace(text[4]))
            text = string.Concat(text.AsSpan(0, 4), text.AsSpan(5));
        if (text.Length != 6 || text[0] < '1' || text[0] > '9')
            return false;
        for (int i = 1; i < 4; i++)
        {
            if (!IsAsciiDigit(text[i]))
                return false;
        }
        if (!IsAsciiLetter(text[4]) || !IsAsciiLetter(text[5]))
            return false;
        // SA, SD and SS are not issued.
        return !(ToUpperAscii(text[4]) == 'S' && ToUpperAscii(text[5]) is 'A' or 'D' or 'S');
    }

    /// <summary>Dutch phone number: 9 significant digits after the <c>0</c>, <c>+31</c> or <c>0031</c> prefix.</summary>
    /// <param name="value">The string to check. Spaces, dashes, dots, slashes and parentheses allowed, e.g. <c>+31 (0)6 12345678</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Dutch phone number.</returns>
    public static bool IsValidNlPhone(string value)
    {
        return TryParsePhone(value, "31", out var national)
            && national.Length == 9;
    }

    /// <summary>Belgian phone number: mobile <c>4[5-9]</c> + 7 digits or 8-digit landline, after the <c>0</c>, <c>+32</c> or <c>0032</c> prefix.</summary>
    /// <param name="value">The string to check. Spaces, dashes, dots, slashes and parentheses allowed, e.g. <c>0475/12.34.56</c>.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Belgian phone number.</returns>
    public static bool IsValidBePhone(string value)
    {
        if (!TryParsePhone(value, "32", out var national))
            return false;
        if (national.Length == 8)
            return true;
        return national.Length == 9 && national[0] == '4' && national[1] >= '5';
    }

    // Reads a phone number for the given country calling code and returns the significant
    // (national) digits without trunk prefix. Accepts +CC, 00CC and national 0 prefixes, an
    // optional trunk 0 after the country code (written as "(0)" or not), and separators.
    private static bool TryParsePhone(string value, string countryCode, out string national)
    {
        national = string.Empty;
        if (value is null)
            return false;
        var text = Trim(value);
        if (text.Length == 0)
            return false;

        bool international = text[0] == '+';
        var digits = new StringBuilder(text.Length);
        bool inGroup = false;
        bool groupHasDigit = false;
        for (int i = international ? 1 : 0; i < text.Length; i++)
        {
            char c = text[i];
            if (IsAsciiDigit(c))
            {
                digits.Append(c);
                groupHasDigit = true;
            }
            else if (c == '(')
            {
                if (inGroup)
                    return false;
                inGroup = true;
                groupHasDigit = false;
            }
            else if (c == ')')
            {
                if (!inGroup || !groupHasDigit)
                    return false;
                inGroup = false;
            }
            else if (!(IsSpace(c) || c == '-' || c == '.' || c == '/'))
            {
                return false;
            }
        }
        if (inGroup)
            return false;

        var all = digits.ToString();
        string rest;
        if (international)
        {
            if (!all.StartsWith(countryCode, StringComparison.Ordinal))
                return false;
            rest = all[countryCode.Length..];
        }
        else if (all.StartsWith("00", StringComparison.Ordinal))
        {
            if (!all.AsSpan(2).StartsWith(countryCode, StringComparison.Ordinal))
                return false;
            rest = all[(2 + countryCode.Length)..];
        }
        else
        {
            if (all.Length < 2 || all[0] != '0')
                return false;
            national = all[1..];
            return true;
        }

        // Optional trunk prefix after the country code: "+31 (0)6 ..." or "+31 06 ...".
        if (rest.StartsWith('0'))
            rest = rest[1..];
        if (rest.Length == 0 || rest[0] == '0')
            return false;
        national = rest;
        return true;
    }

    private static bool PassesElfproef(string nineDigits)
    {
        int sum = 0;
        bool allZero = true;
        for (int i = 0; i < 8; i++)
        {
            int d = nineDigits[i] - '0';
            sum += d * (9 - i);
            allZero &= d == 0;
        }
        int last = nineDigits[8] - '0';
        allZero &= last == 0;
        return !allZero && (sum - last) % 11 == 0;
    }

    // ISO 7064 mod 97-10 over an upper-case alphanumeric string; letters count as 10..35.
    private static int Mod97(string text)
    {
        int remainder = 0;
        foreach (char c in text)
        {
            remainder = IsAsciiDigit(c)
                ? ((remainder * 10) + (c - '0')) % 97
                : ((remainder * 100) + (c - 'A' + 10)) % 97;
        }
        return remainder;
    }

    // Removes separators and upper-cases ASCII letters. Fails on any character that is
    // neither a separator nor allowed, and on an empty result.
    private static bool TryCompact(string value, string separators, bool allowLetters, out string compact)
    {
        compact = string.Empty;
        if (value is null)
            return false;
        var text = Trim(value);
        var builder = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (separators.Contains(c, StringComparison.Ordinal))
                continue;
            if (!(IsAsciiDigit(c) || (allowLetters && IsAsciiLetter(c))))
                return false;
            builder.Append(ToUpperAscii(c));
        }
        compact = builder.ToString();
        return compact.Length > 0;
    }

    private static string Trim(string value) => value.Trim(TrimChars);

    private static bool IsSpace(char c) => c == ' ' || c == '\u00A0';

    private static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';

    private static bool IsAsciiLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

    private static char ToUpperAscii(char c) => c >= 'a' && c <= 'z' ? (char)(c - 32) : c;
}
