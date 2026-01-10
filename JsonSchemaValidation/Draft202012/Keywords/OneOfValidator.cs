using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class OneOfValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "oneOf";

        public OneOfValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            int validCount = 0;

            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContext(context);
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
                : $"Instance validated against {nOk} schemas in 'oneOf', but must match exactly one";

            return ValidationResult.Invalid(instanceLocation, kwLocation, errorMsg) with { Children = children };
        }
    }
}
