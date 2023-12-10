using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class NotValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;

        public NotValidator(ISchemaValidator validator)
        {
            _validator = validator;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if(_validator.Validate(instance) == ValidationResult.Ok)
            {
                return new ValidationResult("Instance should fail to validate against the schema in 'not'.");
            }

            return ValidationResult.Ok;
        }
    }
}