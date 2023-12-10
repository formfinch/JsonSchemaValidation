using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class OneOfValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;

        public OneOfValidator(IEnumerable<ISchemaValidator> validators)
        {
            _validators = validators;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            int nOk = 0;
            foreach(var validator in _validators)
            {
                if (validator.Validate(instance) == ValidationResult.Ok)
                {
                    nOk++;
                    if (nOk > 1)
                    {
                        break;
                    }
                }
            }
            if (nOk == 1)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult("Instance failed to validate against exactly one of the schemas in 'oneOf'.");
        }
    }
}