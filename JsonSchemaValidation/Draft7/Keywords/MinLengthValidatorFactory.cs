// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for minLength keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft7.Keywords
{
    internal class MinLengthValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "minLength";

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

            if (doubleValue < 0 || Math.Abs(doubleValue - Math.Floor(doubleValue)) > double.Epsilon || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'minLength' keyword must have a non-negative integer value.");
            }

            return new MinLengthValidator((int)doubleValue);
        }
    }
}
