// Draft behavior: Identical in Draft 3, Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that the data is a JSON number with no fractional part.

using System.Numerics;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class TypeIntegerValidator : IKeywordValidator
    {
        public static readonly TypeIntegerValidator Instance = new();

        public string Keyword => "type";

        public bool SupportsDirectValidation => true;

        private TypeIntegerValidator() { }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Number)
                return false;

            if (data.TryGetDecimal(out decimal value) && value == decimal.Truncate(value))
                return true;

            if (BigInteger.TryParse(data.ToString(), System.Globalization.CultureInfo.InvariantCulture, out _))
                return true;

            if (data.TryGetDouble(out double doubleValue)
                && !double.IsInfinity(doubleValue)
                && !double.IsNaN(doubleValue)
                && Math.Abs(doubleValue - Math.Floor(doubleValue)) < double.Epsilon)
                return true;

            return false;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Expected an integer value");
        }
    }
}
