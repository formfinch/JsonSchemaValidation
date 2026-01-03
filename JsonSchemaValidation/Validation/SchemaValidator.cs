using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;

namespace JsonSchemaValidation.Validation
{
    public class SchemaValidator : ISchemaValidator
    {
        private readonly List<IKeywordValidator> _keywordValidators = new();

        public void AddKeywordValidator(IKeywordValidator keywordValidator)
        {
            _keywordValidators.Add(keywordValidator);
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var children = new List<ValidationResult>();

            foreach (var validator in _keywordValidators)
            {
                // Each keyword validator gets its own keyword path: parent + keyword name
                var keywordPath = keywordLocation.Append(validator.Keyword);
                var validatorResult = validator.Validate(context, keywordPath);
                children.Add(validatorResult);
            }

            // Aggregate all keyword results
            return ValidationResult.Aggregate(
                context.InstanceLocation.ToString(),
                keywordLocation.ToString(),
                children
            );
        }
    }
}