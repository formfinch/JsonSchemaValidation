using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
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

            if (JsonElement.DeepEquals(_expectedValue, context.Data))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value must equal the const value");
        }
    }
}
