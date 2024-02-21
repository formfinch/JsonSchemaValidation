using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class OneOfValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;
        private IJsonValidationContextFactory _contextFactory;

        public OneOfValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var result = new ValidationResult("Instance failed to validate against exactly one of the schemas in 'oneOf'.");
            int nOk = 0;
            List<IJsonValidationContext> contexts = new();
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CopyContext(context);
                if (validator.Validate(activeContext) == ValidationResult.Ok)
                {
                    contexts.Add(activeContext);
                    nOk++;
                    if (nOk > 1)
                    {
                        break;
                    }
                }
            }
            if (nOk == 1)
            {
                result = ValidationResult.Ok;
                foreach (var activeContext in contexts)
                {
                    _contextFactory.CopyAnnotations(activeContext, context);
                }
            }
            return result;

        }
    }
}