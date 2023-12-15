using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;

namespace JsonSchemaValidation.Validation
{
    public class SchemaValidator : ISchemaValidator
    {
        private readonly List<IKeywordValidator> _keywordValidators = new();

        public void AddKeywordValidator(IKeywordValidator keywordValidator)
        {
            _keywordValidators.Add(keywordValidator);
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var result = new ValidationResult();

            foreach (var validator in _keywordValidators)
            {
                var validatorResult = validator.Validate(context);
                result.Merge(validatorResult);
            }

            if (!result.IsValid)
            {
                return result;
            }

            return ValidationResult.Ok;
        }
    }
}