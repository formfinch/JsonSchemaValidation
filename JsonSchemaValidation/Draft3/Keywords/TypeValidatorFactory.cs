// Draft 3 behavior: Handles single type specification (e.g., "type": "string").
// Supports "any" type which matches any JSON value.

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal class TypeValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "type";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("type", out var typeElement))
            {
                return null;
            }

            if (typeElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? typeSpecification = typeElement.GetString();
            return TypeValidatorSharedFactory.CreateFromTypeSpecification(typeSpecification);
        }
    }
}
