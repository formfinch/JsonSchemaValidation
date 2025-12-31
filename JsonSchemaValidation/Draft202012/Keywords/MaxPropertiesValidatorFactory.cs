using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
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

            if (doubleValue < 0 || doubleValue != Math.Floor(doubleValue) || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'maxProperties' keyword must have a non-negative integer value.");
            }

            return new MaxPropertiesValidator((int)doubleValue);
        }
    }
}
