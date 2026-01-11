using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MaxItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "maxItems";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("maxItems", out var maxItemsElement))
            {
                return null;
            }

            if (maxItemsElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            if (!maxItemsElement.TryGetDouble(out var doubleValue))
            {
                return null;
            }

            if (doubleValue < 0 || Math.Abs(doubleValue - Math.Floor(doubleValue)) > double.Epsilon || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'maxItems' keyword must have a non-negative integer value.");
            }

            return new MaxItemsValidator((int)doubleValue);
        }
    }
}
