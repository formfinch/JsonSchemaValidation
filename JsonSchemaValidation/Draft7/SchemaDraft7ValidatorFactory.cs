using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;
using JsonSchemaValidation.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidation.Draft7
{
    public class SchemaDraft7ValidatorFactory : ISchemaDraftValidatorFactory
    {
        private readonly ISchemaDraftKeywordValidatorFactory[] _keywordFactories;

        public string DraftVersion => SchemaDraft7Setup.DraftVersion;

        public SchemaDraft7ValidatorFactory(
            [FromKeyedServices(SchemaDraft7Setup.DraftVersion)] IEnumerable<ISchemaDraftKeywordValidatorFactory> keywordFactories)
        {
            // Sort factories by ExecutionOrder to guarantee correct execution sequence.
            _keywordFactories = keywordFactories.OrderBy(f => f.ExecutionOrder).ToArray();
        }

        public ISchemaValidator CreateValidator(SchemaMetadata schemaData)
        {
            var schemaValidator = new SchemaValidator();
            var schema = schemaData.Schema;

            // Draft 7 special case: when $ref is present, ALL sibling keywords are ignored.
            // Only the $ref validator should be created.
            if (schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("$ref", out _))
            {
                var refFactory = _keywordFactories.FirstOrDefault(f => string.Equals(f.Keyword, "$ref", StringComparison.Ordinal));
                if (refFactory != null)
                {
                    var refValidator = refFactory.Create(schemaData);
                    if (refValidator != null)
                    {
                        schemaValidator.AddKeywordValidator(refValidator);
                    }
                }
                return schemaValidator;
            }

            // Normal processing for schemas without $ref
            var activeKeywords = schemaData.ActiveKeywords; // null means all keywords are active

            // Loop through each keyword factory to create keyword validators and add them to the schema validator.
            foreach (var keywordFactory in _keywordFactories)
            {
                // Skip if vocabulary filtering is active and this keyword is not included
                // Empty keyword (e.g., BooleanValidatorFactory) is always allowed
                if (activeKeywords != null
                    && !string.IsNullOrEmpty(keywordFactory.Keyword)
                    && !activeKeywords.Contains(keywordFactory.Keyword))
                {
                    continue;
                }

                var keywordValidator = keywordFactory.Create(schemaData);
                if (keywordValidator != null)
                {
                    schemaValidator.AddKeywordValidator(keywordValidator);
                }
            }
            return schemaValidator;
        }
    }
}
