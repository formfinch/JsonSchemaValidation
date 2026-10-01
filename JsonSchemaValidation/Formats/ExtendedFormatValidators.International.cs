// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;

namespace FormFinch.JsonSchemaValidation.Formats;

internal static partial class ExtendedFormatValidators
{
    /// <summary>International phone number (ITU-T E.164): <c>+</c> or <c>00</c>, then 7–15 digits not starting with 0.</summary>
    /// <param name="value">The string to check. Spaces, dashes, dots, slashes and parentheses allowed; one <c>(0)</c> trunk group is ignored.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid international phone number.</returns>
    public static bool IsValidE164Phone(string value)
    {
        if (value is null)
            return false;
        var text = Trim(value);
        int start;
        if (text.StartsWith('+'))
            start = 1;
        else if (text.StartsWith("00", StringComparison.Ordinal))
            start = 2;
        else
            return false;

        var digits = new StringBuilder(text.Length);
        bool inGroup = false;
        int groupStart = 0;
        int groups = 0;
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (IsAsciiDigit(c))
            {
                digits.Append(c);
            }
            else if (c == '(')
            {
                if (inGroup)
                    return false;
                inGroup = true;
                groupStart = digits.Length;
                groups++;
            }
            else if (c == ')')
            {
                if (!inGroup || digits.Length == groupStart)
                    return false;
                inGroup = false;
                // A first group written "(0)" after the country code is a trunk prefix, not a digit.
                if (groups == 1 && groupStart > 0 && digits.Length == groupStart + 1 && digits[groupStart] == '0')
                    digits.Length = groupStart;
            }
            else if (!IsPhoneSeparator(c))
            {
                return false;
            }
        }
        if (inGroup)
            return false;
        return digits.Length >= 7 && digits.Length <= 15 && digits[0] != '0';
    }
}
