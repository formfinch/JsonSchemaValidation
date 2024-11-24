using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class AllOfValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public AllOfValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var result = ValidationResult.Ok;
            var contexts = new List<IJsonValidationContext>();
            int idx = 0;
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CopyContext(context);
                var schemaResult = validator.Validate(activeContext);
                if (schemaResult != ValidationResult.Ok)
                {
                    result = new ValidationResult($"Instance failed to validate against one of the schemas in 'allOf' at index {idx}.");
                    result.Merge(schemaResult);
                    break;
                }
                else
                {
                    contexts.Add(activeContext);
                }
                idx++;
            }

            if (result == ValidationResult.Ok)
            {
                foreach (var activeContext in contexts)
                {
                    _contextFactory.CopyAnnotations(activeContext, context);
                }
            }
            return result;
        }
    }
}