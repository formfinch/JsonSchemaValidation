using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Common
{
    public class SchemaValidatorFactory : ISchemaValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ISchemaRepository _schemaRepository;
        private readonly Dictionary<string, ISchemaDraftValidatorFactory> _draftFactories;

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
            IEnumerable<ISchemaDraftValidatorFactory> draftFactories
        )
        {
            _schemaFactory = schemaFactory;
            _schemaRepository = schemaRepository;
            _draftFactories = draftFactories.ToDictionary(
                draftFactory => draftFactory.DraftVersion,
                draftFactory => draftFactory,
                StringComparer.Ordinal);
        }

        public ISchemaValidator GetValidator(Uri schemaUri)
        {
            var schemaMetaData = _schemaRepository.GetSchema(schemaUri);
            var dereferencedSchema = _schemaFactory.CreateDereferencedSchema(schemaMetaData);
            var validator = CreateValidator(dereferencedSchema);

            // Wrap with scope awareness to push the root schema resource
            return new ScopeAwareSchemaValidator(validator, schemaMetaData);
        }

        public ISchemaValidator CreateValidator(SchemaMetadata schemaMetaData)
        {
            string version = schemaMetaData.DraftVersion!;
            bool usingFallback = false;
            string? originalVersion = null;

            if (!_draftFactories.TryGetValue(version, out ISchemaDraftValidatorFactory? draftFactory))
            {
                // Try fallback for cross-draft compatibility
                if (DraftFallbacks.TryGetValue(version, out string? fallbackVersion) &&
                    _draftFactories.TryGetValue(fallbackVersion, out draftFactory))
                {
                    usingFallback = true;
                    originalVersion = version;
                }
                else
                {
                    throw new NotSupportedException($"Validator for draft version {version} is not supported.");
                }
            }

            // When using fallback, filter keywords to only those that exist in the original draft
            // This ensures keywords introduced in later drafts are ignored
            if (usingFallback && originalVersion != null)
            {
                var allowedKeywords = GetAllowedKeywordsForDraft(originalVersion);
                if (allowedKeywords != null)
                {
                    // Merge with any existing ActiveKeywords filter
                    if (schemaMetaData.ActiveKeywords != null)
                    {
                        schemaMetaData.ActiveKeywords = new HashSet<string>(
                            schemaMetaData.ActiveKeywords.Intersect(allowedKeywords, StringComparer.Ordinal),
                            StringComparer.Ordinal);
                    }
                    else
                    {
                        schemaMetaData.ActiveKeywords = allowedKeywords;
                    }
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
        /// Gets the set of keywords that are valid for a specific draft version.
        /// Used for cross-draft compatibility to filter out keywords introduced in later drafts.
        /// Note: Now that native Draft 3, 4, 6, and 7 implementations exist, this is primarily
        /// for cross-draft schema references where an older draft references a newer draft's keywords.
        /// </summary>
#pragma warning disable S1172 // Method parameter is kept for future extensibility
        private static HashSet<string>? GetAllowedKeywordsForDraft(string draftVersion)
#pragma warning restore S1172
        {
            // Native draft implementations handle their own keyword filtering,
            // so no keyword filtering needed when using fallback to same-family native implementation
            _ = draftVersion; // Suppress unused parameter warning
            return null;
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
