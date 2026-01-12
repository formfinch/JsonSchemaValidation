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
            // Draft 06 -> Draft 7 (mostly compatible, native Draft 7 now available)
            ["http://json-schema.org/draft-06/schema#"] = "http://json-schema.org/draft-07/schema",
            // Draft 04 -> Draft 7 (mostly compatible)
            ["http://json-schema.org/draft-04/schema#"] = "http://json-schema.org/draft-07/schema",
        };

        // Keywords that exist in draft-07 and earlier (used for filtering when falling back)
        // Keywords introduced in 2019-09 or later are NOT included here
        private static readonly HashSet<string> Draft07Keywords = new(StringComparer.Ordinal)
        {
            // Core keywords
            "$id", "$ref", "$schema", "$comment",
            // Definitions (note: $defs is 2019-09+, definitions is draft-07)
            "definitions",
            // Validation keywords
            "type", "enum", "const",
            // Object keywords
            "properties", "additionalProperties", "patternProperties", "propertyNames",
            "required", "minProperties", "maxProperties",
            // Dependencies (single keyword in draft-07, split into dependentRequired/dependentSchemas in 2019-09)
            "dependencies",
            // Array keywords
            "items", "additionalItems", "contains",
            "minItems", "maxItems", "uniqueItems",
            // String keywords
            "minLength", "maxLength", "pattern",
            // Number keywords
            "minimum", "maximum", "exclusiveMinimum", "exclusiveMaximum", "multipleOf",
            // Logic keywords
            "allOf", "anyOf", "oneOf", "not",
            "if", "then", "else",
            // Format
            "format",
            // Metadata keywords
            "title", "description", "default", "examples",
            "readOnly", "writeOnly",
            // Content keywords
            "contentEncoding", "contentMediaType",
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
        /// </summary>
        private static HashSet<string>? GetAllowedKeywordsForDraft(string draftVersion)
        {
            return draftVersion switch
            {
                "http://json-schema.org/draft-07/schema#" => Draft07Keywords,
                "http://json-schema.org/draft-06/schema#" => Draft07Keywords, // Draft 06 is similar to 07
                "http://json-schema.org/draft-04/schema#" => Draft07Keywords, // Draft 04 is a subset of 07
                _ => null
            };
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
