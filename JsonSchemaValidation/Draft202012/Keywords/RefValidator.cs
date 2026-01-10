using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Repositories;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Validator for $ref that resolves the reference at validation time.
    /// In Draft 2020-12, $ref is an applicator that works alongside sibling keywords.
    /// </summary>
    internal sealed class RefValidator : IKeywordValidator
    {
        private readonly string _ref;
        private readonly SchemaMetadata _schemaData;
        private readonly ISchemaRepository _schemaRepository;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public RefValidator(
            string refValue,
            SchemaMetadata schemaData,
            ISchemaRepository schemaRepository,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _ref = refValue;
            _schemaData = schemaData;
            _schemaRepository = schemaRepository;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        // Maximum recursion depth to prevent stack overflow from infinite loops
        private const int MaxRecursionDepth = 100;

        public string Keyword => "$ref";

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            // Resolve the $ref
            var resolvedSchema = ResolveRef();

            if (resolvedSchema == null)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Failed to resolve $ref: {_ref}");
            }

            // Guard against infinite recursion by checking scope depth
            if (context.Scope.Depth > MaxRecursionDepth)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Maximum reference depth exceeded for $ref: {_ref}");
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
            bool pushedScope = false;
            if (resolvedSchema.SchemaUri != null)
            {
                context.Scope.PushSchemaResource(resolvedSchema);
                pushedScope = true;
            }

            try
            {
                // When following $ref, the keyword path resets to the referenced schema's root
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
                if (pushedScope)
                {
                    context.Scope.PopSchemaResource();
                }
            }
        }

        public bool IsValid(IJsonValidationContext context)
        {
            // Resolve the $ref
            var resolvedSchema = ResolveRef();

            if (resolvedSchema == null)
            {
                return false;
            }

            // Guard against infinite recursion by checking scope depth
            if (context.Scope.Depth > MaxRecursionDepth)
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
            bool pushedScope = false;
            if (resolvedSchema.SchemaUri != null)
            {
                context.Scope.PushSchemaResource(resolvedSchema);
                pushedScope = true;
            }

            try
            {
                return validator.IsValid(activeContext);
            }
            finally
            {
                if (pushedScope)
                {
                    context.Scope.PopSchemaResource();
                }
            }
        }

        private SchemaMetadata? ResolveRef()
        {
            if (string.IsNullOrWhiteSpace(_ref))
            {
                return _schemaData;
            }

            if (string.Equals(_ref, "#", StringComparison.Ordinal))
            {
                // Self-reference to root - look up the schema by its base URI (without fragment)
                if (_schemaData.SchemaUri != null)
                {
                    try
                    {
                        // Get the base URI without any fragment
                        var baseUri = new UriBuilder(_schemaData.SchemaUri) { Fragment = string.Empty }.Uri;
                        return _schemaRepository.GetSchema(baseUri);
                    }
                    catch
                    {
                        return _schemaData;
                    }
                }
                return _schemaData;
            }

            if (!Uri.TryCreate(_schemaData.SchemaUri, _ref, out Uri? referenceUri))
            {
                try
                {
                    referenceUri = new Uri(_ref);
                }
                catch
                {
                    return null;
                }
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
    }
}
