using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MaxItemsValidator : IKeywordValidator
    {
        private const string keyword = "maxItems";
        private readonly int maxItems;

        public MaxItemsValidator(int maxItems)
        {
            this.maxItems = maxItems;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the maxItems keyword
                return ValidationResult.Ok;
            }

            if (instance.GetArrayLength() <= maxItems)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }
    }
}