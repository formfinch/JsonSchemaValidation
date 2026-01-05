using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinimumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "minimum";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("minimum", out var minimumElement))
            {
                return null;
            }

            if (!minimumElement.TryGetDouble(out var minimum))
            {
                return null;
            }

            return new MinimumValidator(minimum);
        }
    }
}
