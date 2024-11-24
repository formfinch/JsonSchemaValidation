using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeNullValidator : IKeywordValidator
    {
        public TypeNullValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind == JsonValueKind.Null)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult($"Expected null");
        }
    }
}