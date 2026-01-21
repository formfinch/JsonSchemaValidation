// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for required keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
{
    internal class RequiredValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "required";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("required", out var requiredElement))
            {
                return null;
            }

            // Give an error in case of a single string because this is a common mistake
            // and ignoring the required property could lead to unexpected behavior.
            if (requiredElement.ValueKind == JsonValueKind.String)
            {
                throw new InvalidSchemaException("The 'required' keyword should consist of an array of strings.");
            }

            // Standard behavior is obvious misformatting is ignored
            if (requiredElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            List<string> propertyNames = new List<string>();
            foreach (JsonElement propertyNameElement in requiredElement.EnumerateArray())
            {
                if (propertyNameElement.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidSchemaException("The 'required' keyword should consist of an array of strings.");
                }

                string? propertyName = propertyNameElement.GetString();
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new InvalidSchemaException("The 'required' keyword does not allow for empty property names.");
                }
                propertyNames.Add(propertyName!);
            }

            if (!propertyNames.Any())
            {
                return null;
            }

            return new RequiredValidator(propertyNames);
        }
    }
}
