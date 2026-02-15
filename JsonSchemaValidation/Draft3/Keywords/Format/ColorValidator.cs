// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft 3 specific: Validates CSS color format.
// Supports CSS 2.1 basic color keywords and hex colors (#RGB, #RRGGBB).

using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords.Format
{
    internal sealed partial class ColorValidator : IKeywordValidator
    {
        // CSS 2.1 basic color keywords (case-insensitive)
        private static readonly HashSet<string> CssColorNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "aqua",
            "black",
            "blue",
            "fuchsia",
            "gray",
            "green",
            "lime",
            "maroon",
            "navy",
            "olive",
            "orange",
            "purple",
            "red",
            "silver",
            "teal",
            "white",
            "yellow"
        };

        // Regex for hex color validation: #RGB or #RRGGBB (case-insensitive)
        [GeneratedRegex(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.None, matchTimeoutMilliseconds: 3000)]
        private static partial Regex HexColorRegex();

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            // Format only applies to strings - non-strings are valid
            if (data.ValueKind != JsonValueKind.String)
                return true;

            var str = data.GetString();
            return str == null || IsValidColor(str);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid CSS color");

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = "color" }
            };
        }

        private static bool IsValidColor(string color)
        {
            // Check if it's a valid CSS color name
            if (CssColorNames.Contains(color))
                return true;

            // Check if it's a valid hex color
            if (HexColorRegex().IsMatch(color))
                return true;

            return false;
        }
    }
}
