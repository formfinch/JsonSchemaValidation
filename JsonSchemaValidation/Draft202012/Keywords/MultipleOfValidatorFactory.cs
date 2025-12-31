using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MultipleOfValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "multipleOf";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("multipleOf", out var multipleOfElement))
            {
                return null;
            }

            if (!multipleOfElement.TryGetDouble(out var divisor))
            {
                return null;
            }

            if (divisor <= 0)
            {
                throw new InvalidSchemaException("The 'multipleOf' keyword must have a number value greater than 0.");
            }

            return new MultipleOfValidator(divisor);
        }
    }
}
