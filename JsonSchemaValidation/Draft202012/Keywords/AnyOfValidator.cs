using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class AnyOfValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;

        public AnyOfValidator(IEnumerable<ISchemaValidator> validators)
        {
            _validators = validators;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            foreach (var validator in _validators)
            {
                if(validator.Validate(instance) == ValidationResult.Ok)
                {
                    return ValidationResult.Ok;
                }
            }
            return new ValidationResult("Instance did not validate anyOf schema's.");
        }
    }
}