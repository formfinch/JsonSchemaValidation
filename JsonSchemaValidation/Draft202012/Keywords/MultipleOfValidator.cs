using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MultipleOfValidator : IKeywordValidator
    {
        private const string keyword = "multipleOf";
        private readonly double divisor;

        public MultipleOfValidator(double divisor)
        {
            this.divisor = divisor;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Number) return ValidationResult.Ok;

            // quick check can fail
            double theValue = context.Data.GetDouble();
            if ((theValue % divisor == 0))
            {
                return ValidationResult.Ok;
            }

            // Handle overflow case: for very large numbers that are integers,
            // if divisor evenly divides 1 (like 0.5, 0.25, 0.1), all integers are valid multiples
            double quotient = theValue / divisor;
            if (double.IsInfinity(quotient))
            {
                // Check if the value is an integer and divisor divides 1 evenly
                if (theValue % 1 == 0 && 1.0 % divisor == 0)
                {
                    return ValidationResult.Ok;
                }
            }

            // scaling technique to deal with floating point precision
            quotient = Math.Round((quotient + 0.000001) * 100) / 100.0;
            bool isInteger = Math.Abs(quotient - Math.Round(quotient)) < double.Epsilon;
            if (isInteger) return ValidationResult.Ok;

            return new ValidationResult(keyword);
        }
    }
}
