using System;
using System.Collections.Generic;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Draft202012.Keywords;

namespace JsonSchemaValidation.Implementations.Draft202012
{
    public class SchemaValidator : ISchemaValidator
    {
        private readonly ISchemaRepository _schemaRepository;
        private readonly IEnumerable<IKeywordHandler> _keywordHandlers;

        public SchemaValidator(ISchemaRepository schemaRepository)
        {
            _schemaRepository = schemaRepository;
            _keywordHandlers = new List<IKeywordHandler>
            {
                new TypeKeywordHandler(),
                // Other keyword handlers specific to Draft 2020-12
            };
        }

        public ValidationResult Validate(Uri schemaUri, JsonDocument jsonData)
        {
            using var schema = _schemaRepository.GetSchema(schemaUri);
            if (schema == null)
            {
                throw new InvalidOperationException($"Schema {schemaUri} not found");
            }

            var validationResult = new ValidationResult();

            foreach (var keywordHandler in _keywordHandlers)
            {
                var keywordValidationResult = keywordHandler.Validate(schema.RootElement, jsonData.RootElement);
                validationResult.Merge(keywordValidationResult);
            }

            return validationResult;
        }
    }
}
