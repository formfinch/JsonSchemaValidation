using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class BooleanFalseValidator : IKeywordValidator
    {
        public BooleanFalseValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            return new ValidationResult("Validated against false");
        }
    }
}