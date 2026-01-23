// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Common
{
    /// <summary>
    /// A validator wrapper that manages the validation scope by pushing/popping
    /// schema resources. Used to track the dynamic scope for $dynamicRef resolution.
    /// </summary>
    internal class ScopeAwareSchemaValidator : ISchemaValidator
    {
        private readonly ISchemaValidator _innerValidator;
        private readonly SchemaMetadata _schemaResource;

        public ScopeAwareSchemaValidator(ISchemaValidator innerValidator, SchemaMetadata schemaResource)
        {
            _innerValidator = innerValidator ?? throw new ArgumentNullException(nameof(innerValidator));
            _schemaResource = schemaResource ?? throw new ArgumentNullException(nameof(schemaResource));
        }

        public bool RequiresAnnotationTracking => _innerValidator.RequiresAnnotationTracking;

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

        public bool IsValid(IJsonValidationContext context)
        {
            // Push this schema resource onto the scope
            context.Scope.PushSchemaResource(_schemaResource);

            try
            {
                return _innerValidator.IsValid(context);
            }
            finally
            {
                // Always pop when leaving this schema resource
                context.Scope.PopSchemaResource();
            }
        }
    }
}
