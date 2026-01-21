using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class UniqueItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "uniqueItems";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("uniqueItems", out var uniqueItemsElement))
            {
                return null;
            }

            if (uniqueItemsElement.ValueKind != JsonValueKind.True
                && uniqueItemsElement.ValueKind != JsonValueKind.False)
            {
                throw new InvalidSchemaException("The 'uniqueItems' keyword must have a boolean value.");
            }

            if (uniqueItemsElement.ValueKind == JsonValueKind.False)
            {
                return null;
            }

            return new UniqueItemsValidator();
        }
    }
}
