// Draft behavior: Identical in Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// The const keyword validates that data equals exactly the specified value.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    internal sealed class ConstValidator : IKeywordValidator
    {
        private readonly JsonElement _expectedValue;

        public string Keyword => "const";

        public bool SupportsDirectValidation => true;

        public ConstValidator(JsonElement expectedValue)
        {
            _expectedValue = expectedValue;
        }

        public bool IsValid(JsonElement data) => JsonElement.DeepEquals(_expectedValue, data);

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value must equal the const value");
        }
    }
}
