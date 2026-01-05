using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class UnevaluatedPropertiesValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _unevaluatedPropertyValidator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "unevaluatedProperties";

        public UnevaluatedPropertiesValidator(ISchemaValidator unevaluatedPropertyValidator, IJsonValidationContextFactory contextFactory)
        {
            _unevaluatedPropertyValidator = unevaluatedPropertyValidator;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the unevaluatedProperties keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationObjectContext objectContext)
            {
                throw new InvalidOperationException("Object context is invalid");
            }

            var children = new List<ValidationResult>();
            var invalidProperties = new List<string>();
            var evaluatedProperties = new List<string>();

            foreach (JsonProperty prp in objectContext.GetUnevaluatedProperties())
            {
                var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, prp.Value);
                var validationResult = _unevaluatedPropertyValidator.Validate(prpContext, keywordLocation);
                children.Add(validationResult);

                if (!validationResult.IsValid)
                {
                    invalidProperties.Add(prp.Name);
                }
                else
                {
                    evaluatedProperties.Add(prp.Name);
                }
            }

            if (invalidProperties.Count > 0)
            {
                var props = string.Join(", ", invalidProperties.Select(p => $"'{p}'"));
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Unevaluated properties are invalid: {props}") with { Children = children };
            }

            objectContext.SetUnevaluatedPropertiesEvaluated();

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with property names that were validated by this keyword
            if (evaluatedProperties.Count > 0)
            {
                return result with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = evaluatedProperties }
                };
            }

            return result;
        }
    }
}
