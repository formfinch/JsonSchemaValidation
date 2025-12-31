using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Common
{
    /// <summary>
    /// A validator wrapper that manages the validation scope by pushing/popping
    /// schema resources. Used to track the dynamic scope for $dynamicRef resolution.
    /// </summary>
    public class ScopeAwareSchemaValidator : ISchemaValidator
    {
        private readonly ISchemaValidator _innerValidator;
        private readonly SchemaMetadata _schemaResource;

        public ScopeAwareSchemaValidator(ISchemaValidator innerValidator, SchemaMetadata schemaResource)
        {
            _innerValidator = innerValidator ?? throw new ArgumentNullException(nameof(innerValidator));
            _schemaResource = schemaResource ?? throw new ArgumentNullException(nameof(schemaResource));
        }

        public void AddKeywordValidator(IKeywordValidator keywordValidator)
        {
            // Delegate to inner validator
            _innerValidator.AddKeywordValidator(keywordValidator);
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            // Push this schema resource onto the scope
            context.Scope.PushSchemaResource(_schemaResource);

            try
            {
                return _innerValidator.Validate(context);
            }
            finally
            {
                // Always pop when leaving this schema resource
                context.Scope.PopSchemaResource();
            }
        }
    }
}
