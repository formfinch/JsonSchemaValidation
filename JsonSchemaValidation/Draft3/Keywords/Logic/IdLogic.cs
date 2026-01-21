// Draft 3 behavior: Helper logic for id property parsing.
// Note: Draft 3 uses "id" (without $) instead of "$id".
// Note: Draft 3 allows id with fragments (e.g., "http://example.com/schema#foo").

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Draft3.Keywords.Format;
using FormFinch.JsonSchemaValidation.Exceptions;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords.Logic
{
    internal static class IdLogic
    {
        public static string? GetIdProperty(this JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Draft 3 uses "id" without the $ prefix
            if (!schema.TryGetProperty("id", out var idElement))
            {
                return null;
            }

            // Standard behavior is obvious misformatting is ignored
            if (idElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var idText = idElement.ToString();

            // Empty id is ignored
            if (string.IsNullOrWhiteSpace(idText))
            {
                return null;
            }

            // Draft 3: id can be any valid URI or URI-reference, including with fragments
            var uriValidation = new UriValidationLogic(canBeRelative: true);
            var isValid = uriValidation.IsValidUri(idText);
            if (!isValid)
            {
                throw new InvalidSchemaException("The 'id' keyword must be a string representing a valid URI-reference.");
            }

            return idText;
        }
    }
}
