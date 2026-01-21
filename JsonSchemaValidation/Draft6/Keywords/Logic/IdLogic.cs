// Draft 7 behavior: Helper logic for $id property parsing.
// Note: Draft 7 uses "definitions" instead of "$defs".
// Note: Draft 7 allows $id with plain name fragments like "#foo" which act as anchors.

using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Draft6.Keywords.Format;
using FormFinch.JsonSchemaValidation.Exceptions;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords.Logic
{
    internal static class IdLogic
    {
        // In Draft 7, $id can be:
        // 1. A URI without fragment (like "http://example.com/schema")
        // 2. A relative URI without fragment (like "foo/bar")
        // 3. A plain name fragment (like "#foo") which acts as an anchor
        // But it cannot be a URI with both base and fragment (like "http://example.com#foo")
        private static readonly Regex PlainNameFragmentPattern = new("^#[A-Za-z][A-Za-z0-9.:_-]*$", RegexOptions.Compiled | RegexOptions.NonBacktracking);
        private static readonly Regex IdWithFragmentPattern = new("#.", RegexOptions.Compiled | RegexOptions.NonBacktracking);

        public static string? GetIdProperty(this JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("$id", out var idElement))
            {
                return null;
            }

            // Standard behavior is obvious misformatting is ignored
            if (idElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var idText = idElement.ToString();

            // Draft 7: Plain name fragments (like "#foo") are valid - they act as anchors
            if (idText.StartsWith('#'))
            {
                if (!PlainNameFragmentPattern.IsMatch(idText))
                {
                    throw new InvalidSchemaException("The '$id' plain name fragment must start with a letter and contain only alphanumeric characters, '.', ':', '_', or '-'.");
                }
                return idText;
            }

            // For non-fragment $id values, validate as URI
            var uriValidation = new UriValidationLogic(canBeRelative: true);
            var isValid = uriValidation.IsValidUri(idText);
            if (!isValid)
            {
                throw new InvalidSchemaException("The '$id' keyword must be a string representing a valid URI-reference.");
            }

            // Non-fragment $id cannot have a fragment component (e.g., "http://example.com#foo" is invalid)
            if (IdWithFragmentPattern.IsMatch(idText))
            {
                throw new InvalidSchemaException("The '$id' keyword cannot contain fragments. Use a plain name fragment (e.g., '#foo') or the '$anchor' keyword.");
            }

            return idText;
        }
    }
}
