// Draft behavior: In Draft 3/Draft 4, maximum is a number with optional exclusiveMaximum boolean modifier.
// Validates that numeric data is <= (or <) the maximum value.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class MaximumValidator : IKeywordValidator
    {
        private readonly double _maximum;
        private readonly bool _exclusive;

        public string Keyword => "maximum";

        public bool SupportsDirectValidation => true;

        public MaximumValidator(double maximum, bool exclusive)
        {
            _maximum = maximum;
            _exclusive = exclusive;
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Number)
                return true;

            var value = data.GetDouble();
            return _exclusive ? value < _maximum : value <= _maximum;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            var comparison = _exclusive ? "less than" : "at most";
            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value must be {comparison} {_maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }
    }
}
