using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class UnevaluatedPropertiesValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _unevaluatedPropertyValidator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public UnevaluatedPropertiesValidator(ISchemaValidator unevaluatedPropertyValidator, IJsonValidationContextFactory contextFactory)
        {
            _unevaluatedPropertyValidator = unevaluatedPropertyValidator;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the unevaluatedProperties keyword
                return ValidationResult.Ok;
            }

            if (context is not IJsonValidationObjectContext objectContext)
            {
                throw new InvalidOperationException("Object context is invalid");
            }

            foreach (JsonProperty prp in objectContext.GetUnevaluatedProperties())
            {
                var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, prp.Value);
                var validationResult = _unevaluatedPropertyValidator.Validate(prpContext);
                if (validationResult != ValidationResult.Ok)
                {
                    var propertyNameResult = new ValidationResult($"Property name {prp.Name} is invalid");
                    return propertyNameResult;
                }
            }
            objectContext.SetUnevaluatedPropertiesEvaluated();
            return ValidationResult.Ok;
        }
    }
}