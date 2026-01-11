// Draft behavior: Specific to Draft 2019-09
// Note: In Draft 2020-12, $recursiveRef was replaced by $dynamicRef with different semantics.
// Validator for $recursiveRef that resolves the reference based on $recursiveAnchor.

using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Repositories;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft201909.Keywords
{
    internal sealed class RecursiveRefValidator : IKeywordValidator
    {
        private readonly string _recursiveRef;
        private readonly SchemaMetadata _schemaData;
        private readonly ISchemaRepository _schemaRepository;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "$recursiveRef";

        public RecursiveRefValidator(
            string recursiveRef,
            SchemaMetadata schemaData,
            ISchemaRepository schemaRepository,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _recursiveRef = recursiveRef;
            _schemaData = schemaData;
            _schemaRepository = schemaRepository;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            // Resolve the $recursiveRef based on the current dynamic scope
            var resolvedSchema = ResolveRecursiveRef(context);

            if (resolvedSchema == null)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Failed to resolve $recursiveRef: {_recursiveRef}");
            }

            // Create a validator for the resolved schema
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }

            var validator = _schemaValidatorFactory.Value.CreateValidator(resolvedSchema);

            // Run validation with the current context (scope is shared)
            var activeContext = _contextFactory.CreateFreshContext(context);

            // Push the resolved schema resource onto the scope
            context.Scope.PushSchemaResource(resolvedSchema);
            try
            {
                // When following $recursiveRef, the keyword path resets to the referenced schema's root
                var result = validator.Validate(activeContext, JsonPointer.Empty);

                if (result.IsValid)
                {
                    _contextFactory.CopyAnnotations(activeContext, context);
                }

                // Set absolute keyword location to the referenced schema's URI
                if (resolvedSchema.SchemaUri != null)
                {
                    return new ValidationResult(result, resolvedSchema.SchemaUri);
                }

                return result;
            }
            finally
            {
                // Always pop when leaving the schema resource
                context.Scope.PopSchemaResource();
            }
        }

        public bool IsValid(IJsonValidationContext context)
        {
            // Resolve the $recursiveRef based on the current dynamic scope
            var resolvedSchema = ResolveRecursiveRef(context);

            if (resolvedSchema == null)
            {
                return false;
            }

            // Create a validator for the resolved schema
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }

            var validator = _schemaValidatorFactory.Value.CreateValidator(resolvedSchema);

            // Use tracking if referenced schema needs it, or if parent already tracks
            bool needsTracking = validator.RequiresAnnotationTracking || context is IJsonValidationObjectContext or IJsonValidationArrayContext;
            var activeContext = _contextFactory.CreateFreshContextFast(context, needsTracking);

            // Push the resolved schema resource onto the scope
            context.Scope.PushSchemaResource(resolvedSchema);
            try
            {
                return validator.IsValid(activeContext);
            }
            finally
            {
                context.Scope.PopSchemaResource();
            }
        }

        private SchemaMetadata? ResolveRecursiveRef(IJsonValidationContext context)
        {
            // In Draft 2019-09, $recursiveRef must be "#" and works with $recursiveAnchor
            // $recursiveRef: "#" means: resolve to the nearest schema in the dynamic scope
            // that has $recursiveAnchor: true

            if (!string.Equals(_recursiveRef, "#", StringComparison.Ordinal))
            {
                // Non-# references are just regular $ref behavior
                if (!Uri.TryCreate(_schemaData.SchemaUri, _recursiveRef, out Uri? referenceUri))
                {
                    return null;
                }

                try
                {
                    return _schemaRepository.GetSchema(referenceUri);
                }
                catch
                {
                    return null;
                }
            }

            // Get the static target first (the root of the current schema)
            SchemaMetadata staticTarget;
            try
            {
                if (_schemaData.SchemaUri != null)
                {
                    var baseUri = new UriBuilder(_schemaData.SchemaUri) { Fragment = string.Empty }.Uri;
                    staticTarget = _schemaRepository.GetSchema(baseUri);
                }
                else
                {
                    staticTarget = _schemaData;
                }
            }
            catch
            {
                return _schemaData;
            }

            // Check if the static target has $recursiveAnchor: true
            if (!staticTarget.HasRecursiveAnchor)
            {
                // If static target doesn't have $recursiveAnchor, behave like normal $ref
                return staticTarget;
            }

            // Search the dynamic scope from outermost to innermost for a schema with $recursiveAnchor: true
            var dynamicScope = context.Scope.GetDynamicScope();
            // Use explicit loop to avoid IEnumerable<T> enumerator allocation
            for (int i = 0; dynamicScope.Skip(i).Any(); i++)
            {
                var schemaResource = dynamicScope.ElementAt(i);
                if (schemaResource.HasRecursiveAnchor)
                {
                    return schemaResource;
                }
            }

            // Fall back to static target
            return staticTarget;
        }
    }
}
