using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class MinItemsValidator : IKeywordValidator
    {
        private readonly int _minItems;

        public string Keyword => "minItems";

        public bool SupportsDirectValidation => true;

        public MinItemsValidator(int minItems)
        {
            _minItems = minItems;
        }

        public bool IsValid(JsonElement data) =>
            data.ValueKind != JsonValueKind.Array || data.GetArrayLength() >= _minItems;

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Array has less than the minimum of {_minItems} items");
        }
    }
}
