using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

            double quotient = context.Data.GetDouble() / divisor;

            // scaling technique to deal with floating point precision
            quotient = Math.Round((quotient + 0.000001) * 100) / 100.0;
            bool isInteger = Math.Abs(quotient - Math.Round(quotient)) < double.Epsilon;
            if (isInteger) return ValidationResult.Ok;

            return new ValidationResult(keyword);
        }
    }
}
