// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;
using FormFinch.JsonSchemaValidation.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Draft201909
{
    public class SchemaDraft201909ValidatorFactory : ISchemaDraftValidatorFactory
    {
        private readonly ISchemaDraftKeywordValidatorFactory[] _keywordFactories;

        public string DraftVersion => SchemaDraft201909Setup.DraftVersion;

        public SchemaDraft201909ValidatorFactory(
            [FromKeyedServices(SchemaDraft201909Setup.DraftVersion)] IEnumerable<ISchemaDraftKeywordValidatorFactory> keywordFactories)
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
