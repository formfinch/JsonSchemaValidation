using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class AnyOfValidator : IKeywordValidator
    {
        private readonly ISchemaValidator[] _validators;
        private readonly IJsonValidationContextFactory _contextFactory;
        private readonly bool _requiresTracking;

        public string Keyword => "anyOf";

        public AnyOfValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators.ToArray();
            _contextFactory = contextFactory;
            // Check if any sub-schema requires annotation tracking
            _requiresTracking = _validators.Any(v => v.RequiresAnnotationTracking);
        }

        public bool IsValid(IJsonValidationContext context)
        {
            // Use tracking contexts if any sub-schema needs it, or if parent already tracks
            bool needsTracking = _requiresTracking || context is IJsonValidationObjectContext or IJsonValidationArrayContext;

            // Fast path: short-circuit on first success
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContextFast(context, needsTracking);
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
