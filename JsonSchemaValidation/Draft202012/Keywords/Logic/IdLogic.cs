using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Draft202012.Keywords.Format;
using FormFinch.JsonSchemaValidation.Exceptions;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords.Logic
{
    internal static class IdLogic
    {
        private static readonly Regex IdFragmentPattern = new("^[^#]*#?$", RegexOptions.Compiled | RegexOptions.NonBacktracking);

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

            var uriValidation = new UriValidationLogic(canBeRelative: true);
            var idText = idElement.ToString();
            var isValid = uriValidation.IsValidUri(idText);
            if (!isValid)
            {
                throw new InvalidSchemaException("The '$id' keyword must be a string representing a valid URI - reference.");
            }

            if (!IdFragmentPattern.IsMatch(idText))
            {
                throw new InvalidSchemaException("The '$id' keyword cannot contain fragments. To use fragments, refer to the '$anchor' keyword.");
            }

            return idText;
        }
    }
}
