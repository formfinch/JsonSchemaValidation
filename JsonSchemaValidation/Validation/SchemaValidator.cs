using System;
using System.Collections.Generic;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;

namespace JsonSchemaValidation.Validation
{
    public class SchemaValidator : ISchemaValidator
    {
        private readonly ISchemaRepository _schemaRepository;
        private readonly IEnumerable<IKeywordValidatorFactory> _keywordFactories;
        private readonly Dictionary<Uri, List<IKeywordValidator>> _validatorCache = new();

        public SchemaValidator(ISchemaRepository schemaRepository, IEnumerable<IKeywordValidatorFactory> keywordFactories)
        {
            _schemaRepository = schemaRepository ?? throw new ArgumentNullException(nameof(schemaRepository));
            _keywordFactories = keywordFactories ?? throw new ArgumentNullException(nameof(keywordFactories));
        }

        public ValidationResult Validate(Uri schemaUri, JsonElement jsonData)
        {
            if (schemaUri == null) throw new ArgumentNullException(nameof(schemaUri));
            if (jsonData.ValueKind == JsonValueKind.Undefined)
                throw new ArgumentNullException(nameof(jsonData));

            // Check cache
            if (!_validatorCache.TryGetValue(schemaUri, out var validators))
            {
                var schema = _schemaRepository.GetSchema(schemaUri);
                validators = DetermineValidators(schema);
                _validatorCache[schemaUri] = validators;
            }

            return ValidateUsingValidators(validators, jsonData);
        }

        private List<IKeywordValidator> DetermineValidators(JsonElement schema)
        {
            var validators = new List<IKeywordValidator>();
            foreach (var factory in _keywordFactories)
            {
                var validator = factory.Create(schema);
                if (validator != null)
                    validators.Add(validator);
            }
            return validators;
        }

        private ValidationResult ValidateUsingValidators(List<IKeywordValidator> validators, JsonElement instance)
        {
            var result = new ValidationResult();
            foreach (var validator in validators)
            {
                var validatorResult = validator.Validate(instance);
                result.Merge(validatorResult);
            }
            return result;
        }
    }
}