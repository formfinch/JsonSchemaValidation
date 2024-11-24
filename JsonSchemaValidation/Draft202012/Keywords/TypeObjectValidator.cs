using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeObjectValidator : IKeywordValidator
    {
        private ValidationResult validationFailed = new("Expected an object");

        public TypeObjectValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                return validationFailed;
            }

            return ValidationResult.Ok;
        }
    }
}

