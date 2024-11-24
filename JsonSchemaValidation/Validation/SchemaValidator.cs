using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;

namespace JsonSchemaValidation.Validation
{
    public class SchemaValidator : ISchemaValidator
    {
        private readonly List<IKeywordValidator> _keywordValidators = new();

        public void AddKeywordValidator(IKeywordValidator keywordValidator)
        {
            _keywordValidators.Add(keywordValidator);
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            ValidationResult result = new ();

            foreach (var validator in _keywordValidators)
            {
                var validatorResult = validator.Validate(context);
                result.Merge(validatorResult);
            }

            if (!result.IsValid)
            {
                return result;
            }

            return ValidationResult.Ok;
        }
    }
}