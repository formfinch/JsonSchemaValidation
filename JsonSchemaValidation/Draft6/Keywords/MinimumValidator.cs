// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that numeric data is >= the minimum value.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft6.Keywords
{
    internal sealed class MinimumValidator : IKeywordValidator
    {
        private readonly double _minimum;

        public string Keyword => "minimum";

        public bool SupportsDirectValidation => true;

        public MinimumValidator(double minimum)
        {
            _minimum = minimum;
        }

        public bool IsValid(JsonElement data) =>
            data.ValueKind != JsonValueKind.Number || data.GetDouble() >= _minimum;

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value must be at least {_minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }
    }
}
