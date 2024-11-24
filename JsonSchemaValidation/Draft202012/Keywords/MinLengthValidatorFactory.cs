using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinLengthValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("minLength", out var minLengthElement))
            {
                return null;
            }

            if (minLengthElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            if (!minLengthElement.TryGetDouble(out var doubleValue))
            {
                return null;
            }

            if (doubleValue < 0 || doubleValue != Math.Floor(doubleValue) || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'minLength' keyword must have a non-negative integer value.");
            }

            return new MinLengthValidator((int)doubleValue);
        }
    }
}
