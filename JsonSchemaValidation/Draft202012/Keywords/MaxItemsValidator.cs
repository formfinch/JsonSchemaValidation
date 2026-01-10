using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class MaxItemsValidator : IKeywordValidator
    {
        private readonly int _maxItems;

        public string Keyword => "maxItems";

        public bool SupportsDirectValidation => true;

        public MaxItemsValidator(int maxItems)
        {
            _maxItems = maxItems;
        }

        public bool IsValid(JsonElement data) =>
            data.ValueKind != JsonValueKind.Array || data.GetArrayLength() <= _maxItems;

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Array exceeds the maximum of {_maxItems} items");
        }
    }
}
