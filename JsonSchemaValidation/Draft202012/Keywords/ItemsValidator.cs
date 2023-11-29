using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemsValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;

        public ItemsValidator(IEnumerable<ISchemaValidator> validators)
        {
            _validators = validators;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the Items keyword
                return ValidationResult.Ok;
            }

            int idx = 0;
            foreach (JsonElement element in instance.EnumerateArray())
            {
                var validator = _validators.ElementAt(idx++);
                var itemValidationResult = validator.Validate(instance);
                if (itemValidationResult != ValidationResult.Ok)
                {
                    return itemValidationResult;
                }

                if (idx >= _validators.Count())
                    break;
            }
            return ValidationResult.Ok;
        }
    }
}