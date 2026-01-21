// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal sealed class RegexValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Valid ECMAScript escape characters (after the backslash)
        // Standard escapes: d, D, w, W, s, S, b, B, n, r, t, v, f, 0
        // Control escapes: cA-cZ, ca-cz
        // Hex/Unicode escapes: x, u
        // Unicode property escapes: p, P
        // Literal escapes for special regex chars: \, /, ^, $, ., *, +, ?, (, ), [, ], {, }, |
        // Backreferences: 1-9
        private static readonly HashSet<char> ValidEcmaScriptEscapes = new()
        {
            'd', 'D', 'w', 'W', 's', 'S', 'b', 'B',
            'n', 'r', 't', 'v', 'f', '0',
            'c', 'x', 'u', 'p', 'P',
            '\\', '/', '^', '$', '.', '*', '+', '?',
            '(', ')', '[', ']', '{', '}', '|',
            '1', '2', '3', '4', '5', '6', '7', '8', '9',
            '-' // Valid inside character classes
        };

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            return str == null || IsValidEcmaScriptRegex(str);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid regex");

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = "regex" }
            };
        }

        private static bool IsValidEcmaScriptRegex(string pattern)
        {
            // First check for non-ECMAScript escape sequences
            if (ContainsNonEcmaScriptEscapes(pattern))
            {
                return false;
            }

            // Then verify it's a valid regex
            try
            {
                _ = new Regex(pattern, RegexOptions.ECMAScript, defaultMatchTimeout);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the pattern contains escape sequences that are valid in .NET but not in ECMAScript.
        /// Examples: \a (bell), \e (escape)
        /// </summary>
        private static bool ContainsNonEcmaScriptEscapes(string pattern)
        {
            int i = 0;
            while (i < pattern.Length - 1)
            {
                if (pattern[i] == '\\')
                {
                    // Count preceding backslashes to check if this one is escaped
                    int precedingBackslashes = 0;
                    for (int j = i - 1; j >= 0 && pattern[j] == '\\'; j--)
                    {
                        precedingBackslashes++;
                    }

                    // If odd number of preceding backslashes, this backslash is escaped
                    if (precedingBackslashes % 2 == 1)
                    {
                        i++;
                        continue;
                    }

                    char nextChar = pattern[i + 1];

                    // Check if it's a valid ECMAScript escape
                    if (!ValidEcmaScriptEscapes.Contains(nextChar))
                    {
                        // Not a recognized ECMAScript escape - invalid
                        return true;
                    }

                    // Skip the escape sequence (backslash + next char)
                    i += 2;
                }
                else
                {
                    i++;
                }
            }

            return false;
        }
    }
}
