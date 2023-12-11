using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ContainsValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;

        public ContainsValidator(ISchemaValidator validator)
        {
            _validator = validator;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the contains keyword
                return ValidationResult.Ok;
            }

            var containsResult = new ValidationResult("No array item matches with the 'contains' schema.");
            foreach (JsonElement item in instance.EnumerateArray())
            {
                // push index to evaluateditems


                // todo: suboptimal, because of unevaluateditems keyword,
                // all items must validated against the contains keyword
                var itemValidationResult = _validator.Validate(item);
                if (itemValidationResult == ValidationResult.Ok)
                {
                    containsResult = ValidationResult.Ok;
                }
            }
            return containsResult;
        }
    }
}