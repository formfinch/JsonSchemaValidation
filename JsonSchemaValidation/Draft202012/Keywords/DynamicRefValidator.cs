// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Collections.Concurrent;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Repositories;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Validator for $dynamicRef that resolves the reference at validation time
    /// based on the dynamic scope.
    /// </summary>
    internal sealed class DynamicRefValidator : IKeywordValidator
    {
        private readonly string _dynamicRef;
        private readonly ISchemaRepository _schemaRepository;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        // Pre-parsed URI components (computed once in constructor)
        private readonly Uri? _preResolvedUri;
        private readonly string? _fragment;
        private readonly bool _isJsonPointerFragment;

        // Cache for resolved validators by URI to avoid re-creating validators for the same resolved schema.
        // Thread-safe: validators are stateless and can be reused across validation contexts.
        private readonly ConcurrentDictionary<Uri, ISchemaValidator> _validatorCache = new();

        public string Keyword => "$dynamicRef";

        public DynamicRefValidator(
            string dynamicRef,
            SchemaMetadata schemaData,
            ISchemaRepository schemaRepository,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _dynamicRef = dynamicRef;
            _schemaRepository = schemaRepository;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;

            // Pre-parse the reference URI once (avoids repeated Uri.TryCreate calls)
            if (Uri.TryCreate(schemaData.SchemaUri, dynamicRef, out var resolvedUri))
            {
                _preResolvedUri = resolvedUri;
                _fragment = resolvedUri.Fragment;
                _isJsonPointerFragment = string.IsNullOrEmpty(_fragment) ||
                    _fragment.StartsWith("#/", StringComparison.Ordinal);
            }
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            // Resolve the $dynamicRef based on the current dynamic scope
            var resolvedSchema = ResolveDynamicRef(context);

            if (resolvedSchema == null)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Failed to resolve $dynamicRef: {_dynamicRef}");
            }

            // Get or create cached validator for the resolved schema
            var validator = GetOrCreateValidator(resolvedSchema);

            // Run validation with the current context (scope is shared)
            var activeContext = _contextFactory.CreateFreshContext(context);

            // Push the resolved schema resource onto the scope
            context.Scope.PushSchemaResource(resolvedSchema);
            try
            {
                // When following $dynamicRef, the keyword path resets to the referenced schema's root
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
            // Resolve the $dynamicRef based on the current dynamic scope
            var resolvedSchema = ResolveDynamicRef(context);

            if (resolvedSchema == null)
            {
                return false;
            }

            // Get or create cached validator for the resolved schema
            var validator = GetOrCreateValidator(resolvedSchema);

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

        /// <summary>
        /// Gets a cached validator for the resolved schema URI, or creates and caches a new one.
        /// Validators are stateless and safe to reuse across validation contexts.
        /// </summary>
        private ISchemaValidator GetOrCreateValidator(SchemaMetadata resolvedSchema)
        {
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }

            // Cache by resolved URI to avoid re-creating validators for the same dynamic resolution
            if (resolvedSchema.SchemaUri != null &&
                _validatorCache.TryGetValue(resolvedSchema.SchemaUri, out var cachedValidator))
            {
                return cachedValidator;
            }

            var validator = _schemaValidatorFactory.Value.CreateValidator(resolvedSchema);

            // Cache if we have a URI key
            if (resolvedSchema.SchemaUri != null)
            {
                _validatorCache.TryAdd(resolvedSchema.SchemaUri, validator);
            }

            return validator;
        }

        private SchemaMetadata? ResolveDynamicRef(IJsonValidationContext context)
        {
            // Use pre-parsed URI (computed once in constructor)
            if (_preResolvedUri == null)
            {
                return null;
            }

            // If the fragment is a JSON pointer (starts with #/) or empty, treat as normal $ref
            if (_isJsonPointerFragment)
            {
                return _schemaRepository.GetSchema(_preResolvedUri);
            }

            string fragment = _fragment!;

            // Get the static target first (to check for bookending)
            SchemaMetadata staticTarget;
            try
            {
                staticTarget = _schemaRepository.GetSchema(_preResolvedUri);
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

            // Search the dynamic scope from outermost to innermost using snapshot (O(n) vs O(n^2))
            // Snapshot is in LIFO order (innermost=0, outermost=Length-1), so iterate backwards
            var dynamicScopeSnapshot = context.Scope.GetDynamicScopeSnapshot();
            for (int i = dynamicScopeSnapshot.Length - 1; i >= 0; i--)
            {
                var schemaResource = dynamicScopeSnapshot[i];
                if (schemaResource.DynamicAnchors.TryGetValue(fragment, out var anchoredSchema))
                {
                    // Use shallow view to avoid expensive dictionary copies
                    return SchemaMetadata.CreateShallowView(
                        schemaResource,
                        anchoredSchema,
                        schemaResource.GetUriWithFragment(fragment));
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
