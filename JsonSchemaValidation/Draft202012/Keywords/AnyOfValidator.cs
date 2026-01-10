using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class AnyOfValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "anyOf";

        public AnyOfValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            // Fast path: short-circuit on first success
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContextFast(context);
                if (validator.IsValid(activeContext))
                {
                    return true;
                }
            }

            return false;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            var contexts = new List<IJsonValidationContext>();
            var children = new List<ValidationResult>();
            bool anyValid = false;

            int index = 0;
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContext(context);
                var childKeywordPath = keywordLocation.Append(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                var childResult = validator.Validate(activeContext, childKeywordPath);
                children.Add(childResult);

                if (childResult.IsValid)
                {
                    contexts.Add(activeContext);
                    anyValid = true;
                }
                index++;
            }

            if (anyValid)
            {
                foreach (var activeContext in contexts)
                {
                    _contextFactory.CopyAnnotations(activeContext, context);
                }
                return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children };
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Instance did not validate against any of the schemas in 'anyOf'") with { Children = children };
        }
    }
}
