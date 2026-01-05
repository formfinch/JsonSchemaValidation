using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ExclusiveMinimumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "exclusiveMinimum";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("exclusiveMinimum", out var exclusiveMinimumElement))
            {
                return null;
            }

            if (!exclusiveMinimumElement.TryGetDouble(out var minimum))
            {
                return null;
            }

            return new ExclusiveMinimumValidator(minimum);
        }
    }
}
