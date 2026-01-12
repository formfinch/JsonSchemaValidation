// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that data matches exactly one of the given schemas.

using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft7.Keywords
{
    internal sealed class OneOfValidator : IKeywordValidator
    {
        private readonly ISchemaValidator[] _validators;
        private readonly IJsonValidationContextFactory _contextFactory;
        private readonly bool _requiresTracking;

        public string Keyword => "oneOf";

        public OneOfValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
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

            int validCount = 0;

            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContextFast(context, needsTracking);
                if (validator.IsValid(activeContext))
                {
                    validCount++;
                    // Short-circuit: more than one valid means failure
                    if (validCount > 1)
                    {
                        return false;
                    }
                }
            }

            return validCount == 1;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            int nOk = 0;
            var validContexts = new List<IJsonValidationContext>();
            var children = new List<ValidationResult>();

            int index = 0;
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContext(context);
                var childKeywordPath = keywordLocation.Append(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                var childResult = validator.Validate(activeContext, childKeywordPath);
                children.Add(childResult);

                if (childResult.IsValid)
                {
                    validContexts.Add(activeContext);
                    nOk++;
                    if (nOk > 1)
                    {
                        // Don't break early - collect all results for better error reporting
                    }
                }
                index++;
            }

            if (nOk == 1)
            {
                foreach (var activeContext in validContexts)
                {
                    _contextFactory.CopyAnnotations(activeContext, context);
                }
                return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children };
            }

            string errorMsg = nOk == 0
                ? "Instance did not validate against any of the schemas in 'oneOf'"
                : $"Instance validated against {nOk.ToString(System.Globalization.CultureInfo.InvariantCulture)} schemas in 'oneOf', but must match exactly one";

            return ValidationResult.Invalid(instanceLocation, kwLocation, errorMsg) with { Children = children };
        }
    }
}
