using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ExclusiveMaximumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "exclusiveMaximum";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("exclusiveMaximum", out var exclusiveMaximumElement))
            {
                return null;
            }

            if (!exclusiveMaximumElement.TryGetDouble(out var maximum))
            {
                return null;
            }

            return new ExclusiveMaximumValidator(maximum);
        }
    }
}
