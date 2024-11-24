using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeNumberValidator : IKeywordValidator
    {
        public TypeNumberValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind == JsonValueKind.Number)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult($"Expected a number");
        }
    }
}
