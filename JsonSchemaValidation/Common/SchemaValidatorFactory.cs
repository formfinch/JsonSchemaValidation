// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Common
{
    internal class SchemaValidatorFactory : ISchemaValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ISchemaRepository _schemaRepository;
        private readonly Dictionary<string, ISchemaDraftValidatorFactory> _draftFactories;
        private readonly ICompiledValidatorRegistry? _compiledValidatorRegistry;

        // Fallback mappings for cross-draft compatibility
        // Maps unsupported drafts to the closest supported draft
        private static readonly Dictionary<string, string> DraftFallbacks = new(StringComparer.Ordinal)
        {
            // Draft 07 with fragment -> native Draft 7 (without fragment for internal consistency)
            ["http://json-schema.org/draft-07/schema#"] = "http://json-schema.org/draft-07/schema",
            // Draft 06 with fragment -> native Draft 6 (without fragment for internal consistency)
            ["http://json-schema.org/draft-06/schema#"] = "http://json-schema.org/draft-06/schema",
            // Draft 04 with fragment -> native Draft 4 (without fragment for internal consistency)
            ["http://json-schema.org/draft-04/schema#"] = "http://json-schema.org/draft-04/schema",
            // Draft 03 with fragment -> native Draft 3 (without fragment for internal consistency)
            ["http://json-schema.org/draft-03/schema#"] = "http://json-schema.org/draft-03/schema",
        };

        public SchemaValidatorFactory(
            ISchemaFactory schemaFactory,
            ISchemaRepository schemaRepository,
            IEnumerable<ISchemaDraftValidatorFactory> draftFactories,
            ICompiledValidatorRegistry? compiledValidatorRegistry = null
        )
        {
            _schemaFactory = schemaFactory;
            _schemaRepository = schemaRepository;
            _draftFactories = draftFactories.ToDictionary(
                draftFactory => draftFactory.DraftVersion,
                draftFactory => draftFactory,
                StringComparer.Ordinal);
            _compiledValidatorRegistry = compiledValidatorRegistry;
        }

        // Closure allocation is acceptable here as GetValidator is not in the validation hot path
#pragma warning disable HAA0301, HAA0302 // Closure allocation - acceptable for lazy fallback creation
        public ISchemaValidator GetValidator(Uri schemaUri)
        {
            // Check compiled validator registry first
            if (_compiledValidatorRegistry != null &&
                _compiledValidatorRegistry.TryGetValidator(schemaUri, out var compiledValidator))
            {
                // Return a wrapper that uses compiled validator for IsValid
                // and lazily creates dynamic validator for Validate
                return new CompiledSchemaValidator(
                    compiledValidator,
                    () => CreateDynamicValidator(schemaUri));
            }

            return CreateDynamicValidator(schemaUri);
        }
#pragma warning restore HAA0301, HAA0302

        private ISchemaValidator CreateDynamicValidator(Uri schemaUri)
        {
            var schemaMetaData = _schemaRepository.GetSchema(schemaUri);
            var dereferencedSchema = _schemaFactory.CreateDereferencedSchema(schemaMetaData);
            var validator = CreateValidator(dereferencedSchema);

            // Wrap with scope awareness to push the root schema resource
            return new ScopeAwareSchemaValidator(validator, schemaMetaData);
        }

        // Closure allocation is acceptable here as CreateValidator is not in the validation hot path
#pragma warning disable HAA0301, HAA0302
        public ISchemaValidator CreateValidator(SchemaMetadata schemaMetaData)
        {
            // Check compiled validator registry first
            if (schemaMetaData.SchemaUri != null &&
                _compiledValidatorRegistry != null &&
                _compiledValidatorRegistry.TryGetValidator(schemaMetaData.SchemaUri, out var compiledValidator))
            {
                return new CompiledSchemaValidator(
                    compiledValidator,
                    () => CreateDynamicValidatorInternal(schemaMetaData));
            }

            return CreateDynamicValidatorInternal(schemaMetaData);
        }
#pragma warning restore HAA0301, HAA0302

        private ISchemaValidator CreateDynamicValidatorInternal(SchemaMetadata schemaMetaData)
        {
            string version = schemaMetaData.DraftVersion!;

            if (!_draftFactories.TryGetValue(version, out ISchemaDraftValidatorFactory? draftFactory))
            {
                // Try fallback for cross-draft compatibility
                if (!DraftFallbacks.TryGetValue(version, out string? fallbackVersion) ||
                    !_draftFactories.TryGetValue(fallbackVersion, out draftFactory))
                {
                    throw new NotSupportedException($"Validator for draft version {version} is not supported.");
                }
            }

            // Create the validator using the draft-specific factory
            var validator = draftFactory.CreateValidator(schemaMetaData);

            // Check if this schema has its own $id (making it a distinct schema resource)
            // If so, wrap with ScopeAwareSchemaValidator to manage the dynamic scope
            var schemaId = ExtractIdProperty(schemaMetaData.Schema);
            if (!string.IsNullOrEmpty(schemaId))
            {
                return new ScopeAwareSchemaValidator(validator, schemaMetaData);
            }

            return validator;
        }

        /// <summary>
        /// Extracts $id property value without draft-specific validation.
        /// </summary>
        private static string? ExtractIdProperty(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("$id", out var idElement))
            {
                return null;
            }

            if (idElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return idElement.GetString();
        }
    }
}
