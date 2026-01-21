// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that data does not match the given schema.

using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft4.Keywords
{
    internal sealed class NotValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "not";

        public NotValidator(ISchemaValidator validator, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            // Use tracking if sub-schema needs it, or if parent already tracks
            bool needsTracking = _validator.RequiresAnnotationTracking || context is IJsonValidationObjectContext or IJsonValidationArrayContext;
            var activeContext = _contextFactory.CreateFreshContextFast(context, needsTracking);
            return !_validator.IsValid(activeContext);
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            var activeContext = _contextFactory.CreateFreshContext(context);
            var childResult = _validator.Validate(activeContext, keywordLocation);

            if (!childResult.IsValid)
            {
                // Note: annotations from 'not' subschema should NOT be propagated
                // as the subschema by definition did not match
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Instance should fail to validate against the schema in 'not'");
        }
    }
}
