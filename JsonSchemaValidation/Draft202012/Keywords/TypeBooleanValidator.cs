using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeBooleanValidator : IKeywordValidator
    {
        private ValidationResult validationFailed = new("Expected a boolean value");

        public TypeBooleanValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.False
                && context.Data.ValueKind != JsonValueKind.True)
            {
                return validationFailed;
            }

            return ValidationResult.Ok;
        }
    }
}

