using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeStringValidator : IKeywordValidator
    {
        public TypeStringValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind == JsonValueKind.String)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult($"Expected type string");
        }
    }
}
