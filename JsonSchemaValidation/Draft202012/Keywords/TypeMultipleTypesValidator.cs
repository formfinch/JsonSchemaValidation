using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeMultipleTypesValidator : IKeywordValidator
    {
        private readonly IEnumerable<IKeywordValidator> _validators;

        public TypeMultipleTypesValidator(IEnumerable<IKeywordValidator> validators)
        {
            _validators = validators;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var results = new ValidationResult($"Failed to validate against multiple types");
            foreach(var validator in _validators)
            {
                var result = validator.Validate(context);
                if(result == ValidationResult.Ok)
                {
                    return ValidationResult.Ok;
                }
                results.Merge(result);
            }
            return results;
        }
    }
}
