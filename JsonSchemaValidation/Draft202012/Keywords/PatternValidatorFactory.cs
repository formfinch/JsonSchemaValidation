using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PatternValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "pattern";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("pattern", out var patternElement))
            {
                return null;
            }

            if (patternElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? pattern = patternElement.GetString();
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidSchemaException("The value of this pattern must be a string.");
            }

            var rxPattern = EcmaScriptRegexHelper.CreateEcmaScriptRegex(pattern);
            return new PatternValidator(rxPattern);
        }
    }
}
