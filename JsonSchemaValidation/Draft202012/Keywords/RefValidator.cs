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

        public ValidationResult Validate(IJsonValidationContext context)
        {
            // Resolve the $ref
            var resolvedSchema = ResolveRef();

            if (resolvedSchema == null)
            {
                return new ValidationResult($"Failed to resolve $ref: {_ref}");
            }

            // Create a validator for the resolved schema
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }

            var validator = _schemaValidatorFactory.Value.CreateValidator(resolvedSchema);

            // Run validation with the current context (scope is shared)
            var activeContext = _contextFactory.CopyContext(context);

            // Push the resolved schema resource onto the scope if it has its own $id
            bool pushedScope = false;
            if (resolvedSchema.SchemaUri != null && resolvedSchema.SchemaUri != _schemaData.SchemaUri)
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
            if (string.IsNullOrWhiteSpace(_ref) || _ref == "#")
            {
                // Self-reference to root - return the current schema's root
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
