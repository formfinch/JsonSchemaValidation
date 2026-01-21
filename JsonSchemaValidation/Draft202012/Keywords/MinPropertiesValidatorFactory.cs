using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinPropertiesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "minProperties";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("minProperties", out var minPropertiesElement))
            {
                return null;
            }

            if (minPropertiesElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            if (!minPropertiesElement.TryGetDouble(out var doubleValue))
            {
                return null;
            }

            if (doubleValue < 0 || Math.Abs(doubleValue - Math.Floor(doubleValue)) > double.Epsilon || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'minProperties' keyword must have a non-negative integer value.");
            }

            return new MinPropertiesValidator((int)doubleValue);
        }
    }
}
