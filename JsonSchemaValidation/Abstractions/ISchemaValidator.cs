using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaValidator
    {
        void AddKeywordValidator(IKeywordValidator keywordValidator);
        ValidationResult Validate(IJsonValidationContext context);
    }
}
