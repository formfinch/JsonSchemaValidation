using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeArrayValidator : IKeywordValidator
    {
        private ValidationResult validationFailed = new("Expected an array");

        public TypeArrayValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return validationFailed;
            }

            return ValidationResult.Ok;
        }
    }
}

