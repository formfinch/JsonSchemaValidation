// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for minimum keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft6.Keywords
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
