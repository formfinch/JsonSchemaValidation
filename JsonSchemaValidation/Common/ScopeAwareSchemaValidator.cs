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

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            // Push this schema resource onto the scope
            context.Scope.PushSchemaResource(_schemaResource);

            try
            {
                var result = _innerValidator.Validate(context, keywordLocation);

                // Set absolute keyword location based on this schema's URI
                if (_schemaResource.SchemaUri != null)
                {
                    return new ValidationResult(result, _schemaResource.SchemaUri);
                }

                return result;
            }
            finally
            {
                // Always pop when leaving this schema resource
                context.Scope.PopSchemaResource();
            }
        }
    }
}
