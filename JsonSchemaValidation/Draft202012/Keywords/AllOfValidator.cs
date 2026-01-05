using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class AllOfValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "allOf";

        public AllOfValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            var children = new List<ValidationResult>();
            var contexts = new List<IJsonValidationContext>();
            int idx = 0;
            bool allValid = true;

            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContext(context);
                // Each sub-schema in allOf gets path: /allOf/0, /allOf/1, etc.
                var subSchemaPath = keywordLocation.Append(idx);
                var schemaResult = validator.Validate(activeContext, subSchemaPath);
                children.Add(schemaResult);

                if (!schemaResult.IsValid)
                {
                    allValid = false;
                }
                else
                {
                    contexts.Add(activeContext);
                }
                idx++;
            }

            if (allValid)
            {
                foreach (var activeContext in contexts)
                {
                    _contextFactory.CopyAnnotations(activeContext, context);
                }
            }

            return ValidationResult.Aggregate(instanceLocation, kwLocation, children);
        }
    }
}
