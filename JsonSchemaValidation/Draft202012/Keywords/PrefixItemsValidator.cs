using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PrefixItemsValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;

        public PrefixItemsValidator(IEnumerable<ISchemaValidator> validators)
        {
            _validators = validators;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the PrefixItems keyword
                return ValidationResult.Ok;
            }

            if(!_validators.Any())
            {
                return ValidationResult.Ok;
            }

            int idxValidators = 0;
            foreach (JsonElement item in instance.EnumerateArray())
            {
                // push index to evaluateditems

                var validator = _validators.ElementAt(idxValidators++);
                var itemValidationResult = validator.Validate(item);
                if (itemValidationResult != ValidationResult.Ok)
                {
                    return itemValidationResult;
                }

                if (idxValidators >= _validators.Count())
                    break;
            }
            return ValidationResult.Ok;
        }
    }
}