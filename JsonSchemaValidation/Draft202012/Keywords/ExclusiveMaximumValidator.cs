using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ExclusiveMaximumValidator : IKeywordValidator
    {
        private const string keyword = "exclusiveMaximum";
        private readonly double maximum;

        public ExclusiveMaximumValidator(double maximum)
        {
            this.maximum = maximum;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Number) return ValidationResult.Ok;
            if (context.Data.GetDouble() < maximum) return ValidationResult.Ok;
            return new ValidationResult(keyword);
        }

    }
}
