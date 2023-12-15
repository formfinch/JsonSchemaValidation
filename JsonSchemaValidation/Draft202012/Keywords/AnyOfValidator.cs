using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class AnyOfValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public AnyOfValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var result = new ValidationResult("Instance did not validate anyOf schema's.");
            var contexts = new List<IJsonValidationContext>();
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CopyContext(context);
                if (validator.Validate(activeContext) == ValidationResult.Ok)
                {
                    contexts.Add(activeContext);
                    result = ValidationResult.Ok;
                }
            }

            if(result == ValidationResult.Ok)
            {
                if (context is IJsonValidationArrayContext target)
                {
                    foreach (var activeContext in contexts)
                    {
                        if (activeContext is IJsonValidationArrayContext source)
                        {
                            target.SetAnnotations(source.GetAnnotations());
                        }
                    }
                }
            }
            return result;
        }
    }
}