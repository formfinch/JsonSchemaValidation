using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "minItems";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("minItems", out var minItemsElement))
            {
                return null;
            }

            if (minItemsElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            if (!minItemsElement.TryGetDouble(out var doubleValue))
            {
                return null;
            }

            if (doubleValue < 0 || Math.Abs(doubleValue - Math.Floor(doubleValue)) > double.Epsilon || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'minItems' keyword must have a non-negative integer value.");
            }

            return new MinItemsValidator((int)doubleValue);
        }
    }
}
