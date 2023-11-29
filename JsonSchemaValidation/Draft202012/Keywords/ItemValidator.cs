using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;

        public ItemValidator(ISchemaValidator validator)
        {
            _validator = validator;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the items keyword
                return ValidationResult.Ok;
            }

            foreach (JsonElement element in instance.EnumerateArray())
            {
                var itemValidationResult = _validator.Validate(instance);
                if (itemValidationResult != ValidationResult.Ok)
                {
                    return itemValidationResult;
                }
            }
            return ValidationResult.Ok;
        }
    }
}