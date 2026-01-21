using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
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
