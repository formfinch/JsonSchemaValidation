// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for uniqueItems keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
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
