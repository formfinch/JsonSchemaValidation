// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates hostname and idn-hostname formats per RFC 1123 and IDNA2008.

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords.Format
{
    internal sealed class HostnameValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);
        private static readonly IdnMapping idn = new IdnMapping();

        // Simplified regex pattern for ASCII hostname validation (case-insensitive and compiled)
        private static readonly Regex hostnameRegex = new Regex(
            @"^(([a-z0-9]|[a-z0-9][a-z0-9\-]*[a-z0-9])\.)*([a-z0-9]|[a-z0-9][a-z0-9\-]*[a-z0-9])$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, defaultMatchTimeout);

        private readonly bool performIDNConversion;
        private readonly string _formatName;

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public HostnameValidator(bool isIDNFormat = false)
        {
            performIDNConversion = isIDNFormat;
            _formatName = isIDNFormat ? "idn-hostname" : "hostname";
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            if (string.IsNullOrEmpty(str))
                return false;
            return IsValidHostnameValue(str);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        private bool IsValidHostnameValue(string instanceString)
        {
            // For plain hostname format, reject IDN label separators
            if (!performIDNConversion && instanceString.Any(c => c == '\uFF0E' || c == '\u3002' || c == '\uFF61'))
                return false;

            if (performIDNConversion)
            {
                if (!ValidateIdnContextualRules(instanceString))
                    return false;
                try
                {
                    instanceString = idn.GetAscii(instanceString);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
            else
            {
                if (!ValidateALabels(instanceString))
                    return false;
            }

            return IsValidHostname(instanceString);
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value is not a valid {_formatName}");

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = _formatName }
            };
        }

        private static bool IsValidHostname(string hostname)
        {
            if (hostname.Length > 253 || hostname.Split('.').Any(label => label.Length > 63))
            {
                return false;
            }

            return hostnameRegex.IsMatch(hostname);
        }

        /// <summary>
        /// Validates A-labels (punycode) by decoding them and checking IDNA2008 rules.
        /// </summary>
        private static bool ValidateALabels(string hostname)
        {
            foreach (var label in hostname.Split('.'))
            {
                if (string.IsNullOrEmpty(label)) continue;

                // Check if this is an A-label (punycode)
                if (label.StartsWith("xn--", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Decode the punycode to get the U-label
                        var decoded = idn.GetUnicode(label);

                        // Validate the decoded U-label according to IDNA2008 rules
                        if (!ValidateLabelContextualRules(decoded))
                        {
                            return false;
                        }

                        // Check that the label doesn't begin with a combining mark (RFC 5891 Section 4.2.3.2)
                        if (decoded.Length > 0 && IsCombiningMark(decoded[0]))
                        {
                            return false;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Invalid punycode
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if a character is a combining mark (Unicode categories Mn, Mc, Me).
        /// </summary>
        private static bool IsCombiningMark(char c)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            return category == UnicodeCategory.NonSpacingMark ||      // Mn
                   category == UnicodeCategory.SpacingCombiningMark || // Mc
                   category == UnicodeCategory.EnclosingMark;          // Me
        }

        /// <summary>
        /// Validates IDN contextual rules as defined in RFC 5892.
        /// </summary>
        private static bool ValidateIdnContextualRules(string hostname)
        {
            foreach (var label in hostname.Split('.'))
            {
                if (string.IsNullOrEmpty(label)) continue;
                if (!ValidateLabelContextualRules(label)) return false;
            }
            return true;
        }

        private static bool ValidateLabelContextualRules(string label)
        {
            bool hasNonAscii = false, hasKatakanaMiddleDot = false, hasCjk = false;
            bool hasArabicIndic = false, hasExtendedArabicIndic = false;
            char prev = '\0';

            for (int i = 0; i < label.Length; i++)
            {
                char c = label[i];
                if (c > 127) hasNonAscii = true;

                // DISALLOWED characters (RFC 5892 Section 2.6)
                if (c == '\u302E' || c == '\u302F' || c == '\u0640' || c == '\u07FA' ||
                    c == '\u303B' || (c >= '\u3031' && c <= '\u3035'))
                    return false;

                // Contextual rules requiring position checks
                switch (c)
                {
                    case '\u00B7': // MIDDLE DOT - must be between 'l's
                        if (i == 0 || i == label.Length - 1 ||
                            char.ToLowerInvariant(prev) != 'l' ||
                            char.ToLowerInvariant(label[i + 1]) != 'l')
                            return false;
                        break;
                    case '\u0375': // Greek KERAIA - must be followed by Greek
                        if (i == label.Length - 1 || !IsGreek(label[i + 1]))
                            return false;
                        break;
                    case '\u05F3': // Hebrew GERESH - must be preceded by Hebrew
                    case '\u05F4': // Hebrew GERSHAYIM
                        if (i == 0 || prev < '\u05D0' || prev > '\u05EA')
                            return false;
                        break;
                    case '\u30FB': // KATAKANA MIDDLE DOT - label needs CJK
                        hasKatakanaMiddleDot = true;
                        break;
                }

                // Track character classes for end-of-label checks
                if (c >= '\u0660' && c <= '\u0669') hasArabicIndic = true;
                if (c >= '\u06F0' && c <= '\u06F9') hasExtendedArabicIndic = true;
                if (IsCjk(c)) hasCjk = true;

                prev = c;
            }

            // Arabic-Indic digit mixing not allowed
            if (hasArabicIndic && hasExtendedArabicIndic) return false;

            // Katakana middle dot requires CJK character in label
            if (hasKatakanaMiddleDot && !hasCjk) return false;

            // Check "--" at positions 3-4 for U-labels or decoded A-labels
            if (label.Length >= 4 && label[2] == '-' && label[3] == '-')
            {
                if (hasNonAscii) return false;
                if (label.StartsWith("xn--", StringComparison.OrdinalIgnoreCase))
                {
                    var punycode = label.AsSpan(4);
                    int lastHyphen = punycode.LastIndexOf('-');
                    var basic = lastHyphen >= 0 ? punycode.Slice(0, lastHyphen) : punycode;
                    if (basic.Length >= 4 && basic[2] == '-' && basic[3] == '-')
                        return false;
                }
            }

            return true;
        }

        private static bool IsGreek(char c) =>
            (c >= '\u0370' && c <= '\u03FF') || (c >= '\u1F00' && c <= '\u1FFF');

        private static bool IsCjk(char c) =>
            (c >= '\u3040' && c <= '\u309F') ||  // Hiragana
            (c >= '\u30A0' && c <= '\u30FF' && c != '\u30FB') ||  // Katakana (excluding middle dot)
            (c >= '\u31F0' && c <= '\u31FF') ||  // Katakana extensions
            (c >= '\u3400' && c <= '\u4DBF') ||  // CJK Extension A
            (c >= '\u4E00' && c <= '\u9FFF');    // CJK Unified
    }
}
