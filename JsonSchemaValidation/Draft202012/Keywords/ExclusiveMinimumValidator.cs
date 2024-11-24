using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ExclusiveMinimumValidator : IKeywordValidator
    {
        private const string keyword = "exclusiveMinimum";
        private readonly double minimum;

        public ExclusiveMinimumValidator(double minimum)
        {
            this.minimum = minimum;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Number) return ValidationResult.Ok;
            if (context.Data.GetDouble() > minimum) return ValidationResult.Ok;
            return new ValidationResult(keyword);
        }
    }
}
