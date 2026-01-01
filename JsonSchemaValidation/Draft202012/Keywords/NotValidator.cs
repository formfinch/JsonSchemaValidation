using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class NotValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public NotValidator(ISchemaValidator validator, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var result = new ValidationResult("Instance should fail to validate against the schema in 'not'.");
            var activeContext = _contextFactory.CreateFreshContext(context);

            if (_validator.Validate(activeContext) != ValidationResult.Ok)
            {
                result = ValidationResult.Ok;
                // Note: annotations from 'not' subschema should NOT be propagated
                // as the subschema by definition did not match
            }
            return result;
        }
    }
}