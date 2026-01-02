using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class HostnameValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);
        private static readonly IdnMapping idn = new IdnMapping();

        // Simplified regex pattern for ASCII hostname validation
        private static readonly string asciiHostnamePattern = @"^(([a-z0-9]|[a-z0-9][a-z0-9\-]*[a-z0-9])\.)*([a-z0-9]|[a-z0-9][a-z0-9\-]*[a-z0-9])$";

        private readonly Regex hostnameRegex;
        private readonly bool performIDNConversion;
        private readonly string keyword;

        public HostnameValidator(bool isIDNFormat = false)
        {
            var options = RegexOptions.IgnoreCase; // Hostnames are case-insensitive
            hostnameRegex = new Regex(asciiHostnamePattern, options, defaultMatchTimeout);
            performIDNConversion = isIDNFormat;
            keyword = isIDNFormat ? "format:idn-hostname" : "format:hostname";
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.String)
            {
                return ValidationResult.Ok;
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Ok;
            }

            if (performIDNConversion)
            {
                // Validate contextual rules before IDN conversion (RFC 5892)
                if (!ValidateIdnContextualRules(instanceString))
                {
                    return new ValidationResult(keyword);
                }

                // Attempt to convert possible IDN to ASCII
                try
                {
                    instanceString = idn.GetAscii(instanceString);
                }
                catch (ArgumentException)
                {
                    // If conversion fails, the hostname is invalid
                    return new ValidationResult(keyword);
                }
            }

            if (IsValidHostname(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        private bool IsValidHostname(string hostname)
        {
            if (hostname.Length > 253 || hostname.Split('.').Any(label => label.Length > 63))
            {
                return false;
            }

            return hostnameRegex.IsMatch(hostname);
        }

        /// <summary>
        /// Validates IDN contextual rules as defined in RFC 5892.
        /// </summary>
        private bool ValidateIdnContextualRules(string hostname)
        {
            foreach (var label in hostname.Split('.'))
            {
                if (string.IsNullOrEmpty(label)) continue;
                if (!ValidateLabelContextualRules(label)) return false;
            }
            return true;
        }

        private bool ValidateLabelContextualRules(string label)
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