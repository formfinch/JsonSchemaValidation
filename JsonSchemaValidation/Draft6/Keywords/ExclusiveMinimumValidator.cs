// Draft behavior: Identical in Draft 2019-09, Draft 2020-12 (numeric value)
// Note: In Draft 4-7, exclusiveMinimum was a boolean modifier for minimum.
// Starting with Draft 2019-09, it's a standalone numeric value.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft6.Keywords
{
    internal sealed class ExclusiveMinimumValidator : IKeywordValidator
    {
        private readonly double _minimum;

        public string Keyword => "exclusiveMinimum";

        public bool SupportsDirectValidation => true;

        public ExclusiveMinimumValidator(double minimum)
        {
            _minimum = minimum;
        }

        public bool IsValid(JsonElement data) =>
            data.ValueKind != JsonValueKind.Number || data.GetDouble() > _minimum;

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value must be greater than {_minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }
    }
}
