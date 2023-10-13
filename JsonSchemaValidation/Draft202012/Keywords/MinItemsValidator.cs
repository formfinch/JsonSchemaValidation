using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinItemsValidator : IKeywordValidator
    {
        private const string keyword = "minItems";
        private readonly int minItems;

        public MinItemsValidator(int minItems)
        {
            this.minItems = minItems;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the minItems keyword
                return ValidationResult.Ok;
            }

            if(instance.GetArrayLength() >= minItems)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }
    }
}