using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
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
