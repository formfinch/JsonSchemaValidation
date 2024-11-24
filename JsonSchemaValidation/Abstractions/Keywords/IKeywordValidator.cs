using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Abstractions.Keywords
{
    public interface IKeywordValidator
    {
        ValidationResult Validate(IJsonValidationContext context);
    }
}
