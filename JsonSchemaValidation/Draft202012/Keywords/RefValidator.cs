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
    internal class RefValidator : IKeywordValidator
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

        public ValidationResult Validate(IJsonValidationContext context)
        {
            // Resolve the $ref
            var resolvedSchema = ResolveRef();

            if (resolvedSchema == null)
            {
                return new ValidationResult($"Failed to resolve $ref: {_ref}");
            }

            // Guard against infinite recursion by checking scope depth
            if (context.Scope.Depth > MaxRecursionDepth)
            {
                return new ValidationResult($"Maximum reference depth exceeded for $ref: {_ref}");
            }

            // Create a validator for the resolved schema
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }

            var validator = _schemaValidatorFactory.Value.CreateValidator(resolvedSchema);

            // Run validation with the current context (scope is shared)
            var activeContext = _contextFactory.CopyContext(context);

            // Push the resolved schema resource onto the scope
            bool pushedScope = false;
            if (resolvedSchema.SchemaUri != null)
            {
                context.Scope.PushSchemaResource(resolvedSchema);
                pushedScope = true;
            }

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

            if (_ref == "#")
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
