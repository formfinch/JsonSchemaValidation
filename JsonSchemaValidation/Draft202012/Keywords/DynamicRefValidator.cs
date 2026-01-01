using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Validator for $dynamicRef that resolves the reference at validation time
    /// based on the dynamic scope.
    /// </summary>
    internal class DynamicRefValidator : IKeywordValidator
    {
        private readonly string _dynamicRef;
        private readonly SchemaMetadata _schemaData;
        private readonly ISchemaRepository _schemaRepository;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public DynamicRefValidator(
            string dynamicRef,
            SchemaMetadata schemaData,
            ISchemaRepository schemaRepository,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _dynamicRef = dynamicRef;
            _schemaData = schemaData;
            _schemaRepository = schemaRepository;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            // Resolve the $dynamicRef based on the current dynamic scope
            var resolvedSchema = ResolveDynamicRef(context);

            if (resolvedSchema == null)
            {
                return new ValidationResult($"Failed to resolve $dynamicRef: {_dynamicRef}");
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
                var result = validator.Validate(activeContext);

                if (result == ValidationResult.Ok)
                {
                    _contextFactory.CopyAnnotations(activeContext, context);
                }

                return result;
            }
            finally
            {
                // Always pop when leaving the schema resource
                context.Scope.PopSchemaResource();
            }
        }

        private SchemaMetadata? ResolveDynamicRef(IJsonValidationContext context)
        {
            // Parse the reference to get the anchor fragment
            // $dynamicRef can be like "#anchor" or "otherSchema#anchor"

            if (!Uri.TryCreate(_schemaData.SchemaUri, _dynamicRef, out Uri? referenceUri))
            {
                return null;
            }

            string fragment = referenceUri.Fragment;

            // If the fragment is a JSON pointer (starts with #/), treat as normal $ref
            if (string.IsNullOrEmpty(fragment) || fragment.StartsWith("#/"))
            {
                return _schemaRepository.GetSchema(referenceUri);
            }

            // Get the static target first (to check for bookending)
            SchemaMetadata staticTarget;
            try
            {
                staticTarget = _schemaRepository.GetSchema(referenceUri);
            }
            catch
            {
                return null;
            }

            // Bookending check: does the static target have a matching $dynamicAnchor?
            // If not, behave like a normal $ref
            if (!HasMatchingDynamicAnchor(staticTarget, fragment))
            {
                return staticTarget;
            }

            // Search the dynamic scope from outermost to innermost
            foreach (var schemaResource in context.Scope.GetDynamicScope())
            {
                if (schemaResource.DynamicAnchors.TryGetValue(fragment, out var anchoredSchema))
                {
                    return new SchemaMetadata(schemaResource)
                    {
                        Schema = anchoredSchema,
                        SchemaUri = new Uri(schemaResource.SchemaUri!, fragment)
                    };
                }
            }

            // Fall back to static target
            return staticTarget;
        }

        private static bool HasMatchingDynamicAnchor(SchemaMetadata schema, string fragment)
        {
            return schema.DynamicAnchors.ContainsKey(fragment);
        }
    }
}
