using JsonSchemaValidation.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords.Logic
{
    internal static class AnchorLogic
    {
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
            var rxPattern = new Regex($"^[A-Za-z_][-A-Za-z0-9._]*$");
            if (!rxPattern.IsMatch(anchorText))
            {
                throw new InvalidSchemaException("The '$anchor' keyword should be a short text value describing the current URI context.");
            }

            return anchorText;
        }
    }
}
