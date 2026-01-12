// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for maxProperties keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft7.Keywords
{
    internal class MaxPropertiesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "maxProperties";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("maxProperties", out var maxPropertiesElement))
            {
                return null;
            }

            if (maxPropertiesElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            if (!maxPropertiesElement.TryGetDouble(out var doubleValue))
            {
                return null;
            }

            if (doubleValue < 0 || Math.Abs(doubleValue - Math.Floor(doubleValue)) > double.Epsilon || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'maxProperties' keyword must have a non-negative integer value.");
            }

            return new MaxPropertiesValidator((int)doubleValue);
        }
    }
}
