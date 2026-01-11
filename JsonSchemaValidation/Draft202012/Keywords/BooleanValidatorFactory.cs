using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class BooleanValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        // Boolean schemas are not a keyword but a core schema feature, use empty string
        public string Keyword => "";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind == JsonValueKind.False)
            {
                return new BooleanFalseValidator();
            }

            if (schema.ValueKind == JsonValueKind.True)
            {
                return new BooleanTrueValidator();
            }

            return null;
        }
    }
}
