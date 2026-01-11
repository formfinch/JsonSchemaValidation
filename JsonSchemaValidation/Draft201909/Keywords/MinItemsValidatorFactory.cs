// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for minItems keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft201909.Keywords
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
