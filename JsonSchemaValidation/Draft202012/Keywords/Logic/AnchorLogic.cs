// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Exceptions;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords.Logic
{
    internal static class AnchorLogic
    {
        private static readonly Regex AnchorPattern = new("^[A-Za-z_][-A-Za-z0-9._]*$", RegexOptions.Compiled | RegexOptions.NonBacktracking);

        public static string? GetAnchorProperty(this JsonElement schema)
        {
            return GetAnchorKeywordProperty(schema, "$anchor");
        }

        public static string? GetDynamicAnchorProperty(this JsonElement schema)
        {
            return GetAnchorKeywordProperty(schema, "$dynamicAnchor");
        }

        private static string? GetAnchorKeywordProperty(JsonElement schema, string anchorKeyword)
        {
            if (!schema.TryGetProperty(anchorKeyword, out var anchorElement))
            {
                return null;
            }

            // Standard behavior is obvious misformatting is ignored
            if (anchorElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var anchorText = anchorElement.ToString();
            if (!AnchorPattern.IsMatch(anchorText))
            {
                throw new InvalidSchemaException("The '$anchor' keyword should be a short text value describing the current URI context.");
            }

            return anchorText;
        }
    }
}
