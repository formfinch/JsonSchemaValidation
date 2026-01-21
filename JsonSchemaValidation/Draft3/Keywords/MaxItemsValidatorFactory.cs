// Draft behavior: Identical in Draft 3, Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for maxItems keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
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
