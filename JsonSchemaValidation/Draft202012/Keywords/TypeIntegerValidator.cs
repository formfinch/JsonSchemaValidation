using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Numerics;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeIntegerValidator : IKeywordValidator
    {
        private ValidationResult validationFailed = new("Expected an integer value");

        public TypeIntegerValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Number)
            {
                return validationFailed;
            }

            if (context.Data.TryGetDecimal(out decimal value))
            {
                if (value == decimal.Truncate(value))
                {
                    return ValidationResult.Ok;
                }
            }

            if (BigInteger.TryParse(context.Data.ToString(), out _))
            {
                return ValidationResult.Ok;
            }

            return validationFailed;
        }
    }
}

