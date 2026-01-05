using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MaxItemsValidator : IKeywordValidator
    {
        private readonly int _maxItems;

        public string Keyword => "maxItems";

        public MaxItemsValidator(int maxItems)
        {
            _maxItems = maxItems;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the maxItems keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            int arrayLength = context.Data.GetArrayLength();
            if (arrayLength <= _maxItems)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Array has {arrayLength} items, which exceeds the maximum of {_maxItems}");
        }
    }
}
