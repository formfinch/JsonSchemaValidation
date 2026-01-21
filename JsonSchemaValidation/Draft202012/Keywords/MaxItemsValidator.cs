using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
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

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Array exceeds the maximum of {_maxItems.ToString(System.Globalization.CultureInfo.InvariantCulture)} items");
        }
    }
}
