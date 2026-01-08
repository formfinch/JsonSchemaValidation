using System.Numerics;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeIntegerValidator : IKeywordValidator
    {
        public string Keyword => "type";

        public TypeIntegerValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Number)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Expected an integer value");
            }

            if (context.Data.TryGetDecimal(out decimal value)
                && value == decimal.Truncate(value))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (BigInteger.TryParse(context.Data.ToString(), out _))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            // Handle very large numbers that can't fit in decimal or BigInteger parse
            // (e.g., 1e308 which comes as "1E+308" string)
            // Check if the double value is a whole number
            if (context.Data.TryGetDouble(out double doubleValue)
                && !double.IsInfinity(doubleValue)
                && !double.IsNaN(doubleValue)
                && Math.Abs(doubleValue - Math.Floor(doubleValue)) < double.Epsilon)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Expected an integer value");
        }
    }
}
