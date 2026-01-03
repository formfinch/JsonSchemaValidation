using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MultipleOfValidator : IKeywordValidator
    {
        private readonly double _divisor;

        public string Keyword => "multipleOf";

        public MultipleOfValidator(double divisor)
        {
            _divisor = divisor;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Number)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            // quick check can fail
            double theValue = context.Data.GetDouble();
            if ((theValue % _divisor == 0))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            // Handle overflow case: for very large numbers that are integers,
            // if divisor evenly divides 1 (like 0.5, 0.25, 0.1), all integers are valid multiples
            double quotient = theValue / _divisor;
            if (double.IsInfinity(quotient))
            {
                // Check if the value is an integer and divisor divides 1 evenly
                if (theValue % 1 == 0 && 1.0 % _divisor == 0)
                {
                    return ValidationResult.Valid(instanceLocation, kwLocation);
                }
            }

            // scaling technique to deal with floating point precision
            quotient = Math.Round((quotient + 0.000001) * 100) / 100.0;
            bool isInteger = Math.Abs(quotient - Math.Round(quotient)) < double.Epsilon;
            if (isInteger)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value is not a multiple of {_divisor}");
        }
    }
}
