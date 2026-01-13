// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// ECMAScript-compatible regex handling for JSON Schema pattern validation.

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft6.Keywords
{
    /// <summary>
    /// Helper for creating ECMAScript-compatible regular expressions.
    /// JSON Schema patterns follow ECMA 262 regex semantics where:
    /// - \d, \D match ASCII digits only [0-9]
    /// - \w, \W match ASCII word characters only [A-Za-z0-9_]
    /// - \s, \S match ECMAScript whitespace (Unicode whitespace characters)
    /// - \p{...} Unicode property escapes are supported (ES2018+)
    /// </summary>
    internal static class EcmaScriptRegexHelper
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Cache for compiled regex instances keyed by original pattern string.
        /// Thread-safe via ConcurrentDictionary. Patterns are immutable so caching is safe.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.Ordinal);

        // ECMAScript whitespace includes: space, tab, vertical tab, form feed,
        // line terminators (LF, CR, LS, PS), and Unicode category Zs (space separators), plus BOM
        // This pattern matches the ECMAScript definition of whitespace
        private const string EcmaScriptWhitespaceClass = @"[\t\n\v\f\r \u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000\ufeff]";
        private const string EcmaScriptNonWhitespaceClass = @"[^\t\n\v\f\r \u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000\ufeff]";

        // Mapping from ECMAScript/Unicode long property names to .NET equivalents
        private static readonly Dictionary<string, string> PropertyNameMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Letter", "L" },
            { "digit", "Nd" },
            { "Number", "N" },
            { "Punctuation", "P" },
            { "Symbol", "S" },
            { "Mark", "M" },
            { "Separator", "Z" },
            { "Other", "C" },
            { "Uppercase_Letter", "Lu" },
            { "Lowercase_Letter", "Ll" },
            { "Titlecase_Letter", "Lt" },
            { "Modifier_Letter", "Lm" },
            { "Other_Letter", "Lo" },
        };

        /// <summary>
        /// Creates a Regex with ECMAScript-compatible behavior.
        /// Results are cached by pattern string for reuse across schema instances.
        /// </summary>
        public static Regex CreateEcmaScriptRegex(string pattern)
        {
            return RegexCache.GetOrAdd(pattern, static p =>
            {
                string transformedPattern = TransformPattern(p);
                return new Regex(transformedPattern, RegexOptions.None, DefaultTimeout);
            });
        }

        /// <summary>
        /// Transforms the pattern for ECMAScript compatibility:
        /// - \d, \D → ASCII digit equivalents
        /// - \w, \W → ASCII word char equivalents
        /// - \s, \S → ECMAScript whitespace equivalents
        /// - \p{name} → .NET compatible property names
        /// - Non-BMP characters → grouped surrogate pairs for proper quantifier handling
        /// </summary>
        private static string TransformPattern(string pattern)
        {
            var result = new System.Text.StringBuilder();
            int i = 0;

            while (i < pattern.Length)
            {
                // Handle surrogate pairs (non-BMP characters like emojis)
                // These need to be grouped so quantifiers apply to the whole character
                if (char.IsHighSurrogate(pattern[i]) && i + 1 < pattern.Length && char.IsLowSurrogate(pattern[i + 1]))
                {
                    char high = pattern[i];
                    char low = pattern[i + 1];

                    // Check if followed by a quantifier
                    bool hasQuantifier = i + 2 < pattern.Length && IsQuantifier(pattern[i + 2]);

                    if (hasQuantifier)
                    {
                        // Wrap in non-capturing group so quantifier applies to whole character
                        result.Append("(?:");
                        AppendUnicodeEscape(result, high);
                        AppendUnicodeEscape(result, low);
                        result.Append(')');
                    }
                    else
                    {
                        // No quantifier, just output the surrogate pair as escape sequences
                        // This ensures consistent handling
                        AppendUnicodeEscape(result, high);
                        AppendUnicodeEscape(result, low);
                    }
                    i += 2;
                    continue;
                }

                if (pattern[i] == '\\' && i + 1 < pattern.Length)
                {
                    // Count preceding backslashes to check if this one is escaped
                    int precedingBackslashes = CountPrecedingBackslashes(result);

                    // If even number of preceding backslashes, this is a real escape
                    if (precedingBackslashes % 2 == 0)
                    {
                        char nextChar = pattern[i + 1];
                        switch (nextChar)
                        {
                            case 'd':
                                result.Append("[0-9]");
                                i += 2;
                                continue;
                            case 'D':
                                result.Append("[^0-9]");
                                i += 2;
                                continue;
                            case 'w':
                                result.Append("[A-Za-z0-9_]");
                                i += 2;
                                continue;
                            case 'W':
                                result.Append("[^A-Za-z0-9_]");
                                i += 2;
                                continue;
                            case 's':
                                result.Append(EcmaScriptWhitespaceClass);
                                i += 2;
                                continue;
                            case 'S':
                                result.Append(EcmaScriptNonWhitespaceClass);
                                i += 2;
                                continue;
                            case 'p':
                            case 'P':
                                // Handle Unicode property escape \p{...} or \P{...}
                                if (i + 2 < pattern.Length && pattern[i + 2] == '{')
                                {
                                    int closeBrace = pattern.IndexOf('}', i + 3);
                                    if (closeBrace > 0)
                                    {
                                        string propertyName = pattern.Substring(i + 3, closeBrace - i - 3);
                                        string mappedName = MapPropertyName(propertyName);
                                        result.Append('\\');
                                        result.Append(nextChar);
                                        result.Append('{');
                                        result.Append(mappedName);
                                        result.Append('}');
                                        i = closeBrace + 1;
                                        continue;
                                    }
                                }
                                break;
                        }
                    }
                }

                result.Append(pattern[i]);
                i++;
            }

            return result.ToString();
        }

        /// <summary>
        /// Appends a Unicode escape sequence (\uXXXX) for a character without boxing.
        /// </summary>
        private static void AppendUnicodeEscape(System.Text.StringBuilder sb, char c)
        {
            sb.Append("\\u");
            sb.Append(((int)c).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Checks if a character is a regex quantifier.
        /// </summary>
        private static bool IsQuantifier(char c)
        {
            return c == '*' || c == '+' || c == '?' || c == '{';
        }

        /// <summary>
        /// Counts trailing backslashes in the StringBuilder.
        /// </summary>
        private static int CountPrecedingBackslashes(System.Text.StringBuilder sb)
        {
            int count = 0;
            for (int i = sb.Length - 1; i >= 0 && sb[i] == '\\'; i--)
            {
                count++;
            }
            return count;
        }

        /// <summary>
        /// Maps Unicode property long names to .NET regex equivalents.
        /// </summary>
        private static string MapPropertyName(string propertyName)
        {
            if (PropertyNameMapping.TryGetValue(propertyName, out string? mapped))
            {
                return mapped;
            }
            // Return as-is if no mapping found (might be a valid .NET property name already)
            return propertyName;
        }
    }
}
