using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class MaximumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "maximum";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("maximum", out var maximumElement))
            {
                return null;
            }

            if (!maximumElement.TryGetDouble(out var maximum))
            {
                return null;
            }

            return new MaximumValidator(maximum);
        }
    }
}
