using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

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
            var activeContext = _contextFactory.CopyContext(context);

            if (_validator.Validate(activeContext) != ValidationResult.Ok)
            {
                result = ValidationResult.Ok;
                _contextFactory.CopyAnnotations(activeContext, context);
            }
            return result;
        }
    }
}