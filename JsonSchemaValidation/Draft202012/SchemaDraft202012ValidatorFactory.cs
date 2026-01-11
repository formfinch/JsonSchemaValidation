using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;
using JsonSchemaValidation.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidation.Draft202012
{
    public class SchemaDraft202012ValidatorFactory : ISchemaDraftValidatorFactory
    {
        private readonly ISchemaDraftKeywordValidatorFactory[] _keywordFactories;

        public string DraftVersion => SchemaDraft202012Setup.DraftVersion;

        public SchemaDraft202012ValidatorFactory(
            [FromKeyedServices(SchemaDraft202012Setup.DraftVersion)] IEnumerable<ISchemaDraftKeywordValidatorFactory> keywordFactories)
        {
            // Sort factories by ExecutionOrder to guarantee correct execution sequence.
            // This ensures unevaluated keywords (with higher ExecutionOrder) run after other applicators.
            _keywordFactories = keywordFactories.OrderBy(f => f.ExecutionOrder).ToArray();
        }

        public ISchemaValidator CreateValidator(SchemaMetadata schemaData)
        {
            var schemaValidator = new SchemaValidator();
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
