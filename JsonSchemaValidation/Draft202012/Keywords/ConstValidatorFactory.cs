using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class ConstValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "const";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("const", out var constElement))
            {
                return null;
            }

            if (constElement.ValueKind == JsonValueKind.Undefined)
            {
                throw new InvalidSchemaException("const cannot be undefined.");
            }

            return new ConstValidator(constElement);
        }
    }
}
