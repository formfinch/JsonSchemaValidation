// Draft behavior: Identical in Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for boolean schema validators (true/false schemas).

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft4.Keywords
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
