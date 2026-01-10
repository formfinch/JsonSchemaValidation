using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class ExclusiveMaximumValidator : IKeywordValidator
    {
        private readonly double _maximum;

        public string Keyword => "exclusiveMaximum";

        public bool SupportsDirectValidation => true;

        public ExclusiveMaximumValidator(double maximum)
        {
            _maximum = maximum;
        }

        public bool IsValid(JsonElement data) =>
            data.ValueKind != JsonValueKind.Number || data.GetDouble() < _maximum;

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value must be less than {_maximum}");
        }
    }
}
