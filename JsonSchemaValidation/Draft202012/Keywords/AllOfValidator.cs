using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class AllOfValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;

        public AllOfValidator(IEnumerable<ISchemaValidator> validators)
        {
            _validators = validators;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            int idx = 0;
            foreach(var validator in _validators)
            {
                var schemaResult = validator.Validate(instance);
                if (schemaResult != ValidationResult.Ok)
                {
                    var result = new ValidationResult($"Instance failed to validate against one of the schemas in 'allOf' at index {idx}.");
                    result.Merge(schemaResult);
                    return result;
                }
                idx++;
            }
            return ValidationResult.Ok;
        }
    }
}