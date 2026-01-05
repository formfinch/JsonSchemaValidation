using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MaxLengthValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "maxLength";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("maxLength", out var maxLengthElement))
            {
                return null;
            }

            if (maxLengthElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            if (!maxLengthElement.TryGetDouble(out var doubleValue))
            {
                return null;
            }

            if (doubleValue < 0 || doubleValue != Math.Floor(doubleValue) || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'maxLength' keyword must have a non-negative integer value.");
            }

            return new MaxLengthValidator((int)doubleValue);
        }
    }
}
