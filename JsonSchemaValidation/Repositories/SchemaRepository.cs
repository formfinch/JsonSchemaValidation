using System.Collections.Concurrent;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Draft202012.Keywords.Logic;
using JsonSchemaValidation.Exceptions;

namespace JsonSchemaValidation.Repositories
{
    public class SchemaRepository : ISchemaRepository
    {
        private readonly ConcurrentDictionary<Uri, SchemaMetadata> _schemas = new();
        private volatile IReadOnlyList<SchemaMetadata>? _sortedSchemas;
        private readonly Dictionary<string, IVocabularyParser> _vocabularyParsers;
        private readonly string _defaultDraftVersion;

        public SchemaRepository(SchemaValidationOptions options, IEnumerable<IVocabularyParser>? vocabularyParsers = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            _defaultDraftVersion = options.DefaultDraftVersion;
            _vocabularyParsers = new Dictionary<string, IVocabularyParser>(StringComparer.Ordinal);
            if (vocabularyParsers != null)
            {
                // Use List to avoid IEnumerable<T> enumerator allocation
                var parsersList = vocabularyParsers is IList<IVocabularyParser> list ? list : vocabularyParsers.ToList();
                for (int i = 0; i < parsersList.Count; i++)
                {
                    var parser = parsersList[i];
                    _vocabularyParsers[parser.DraftVersion] = parser;
                }
            }
        }

        public bool TryRegisterSchema(JsonElement? schemaToRegister, out SchemaMetadata? schemaData)
        {
            return TryRegisterSchemaInternal(schemaToRegister, null, out schemaData);
        }

        public bool TryRegisterSchema(JsonElement? schemaToRegister, Uri schemaUri, out SchemaMetadata? schemaData)
        {
            return TryRegisterSchemaInternal(schemaToRegister, schemaUri, out schemaData);
        }

        private bool TryRegisterSchemaInternal(JsonElement? schemaToRegister, Uri? explicitUri, out SchemaMetadata? schemaData)
        {
            if (schemaToRegister == null)
            {
                schemaData = null;
                return false;
            }

            var schema = schemaToRegister!.Value;
            if (schema.ValueKind != JsonValueKind.Object
                && schema.ValueKind != JsonValueKind.False
                && schema.ValueKind != JsonValueKind.True)
            {
                schemaData = null;
                return false;
            }

            schemaData = new SchemaMetadata(schema);

            // Check for $recursiveAnchor: true (Draft 2019-09 feature)
            schemaData.HasRecursiveAnchor = HasRecursiveAnchorProperty(schema);

            // Use configured default draft version if not specified
            schemaData.DraftVersion ??= _defaultDraftVersion;

            // Parse $vocabulary if present (for meta-schemas)
            // Select the vocabulary parser based on the draft version
            if (_vocabularyParsers.TryGetValue(schemaData.DraftVersion, out var vocabularyParser))
            {
                var vocabResult = vocabularyParser.ParseVocabulary(schema);
                if (vocabResult != null)
                {
                    schemaData.ActiveVocabularies = vocabResult.Vocabularies;
                    schemaData.ActiveKeywords = vocabResult.ActiveKeywords;
                }
            }

            // Use explicit URI if provided, otherwise use $id or generate random
            if (explicitUri != null)
            {
                schemaData.SchemaUri = explicitUri;
            }
            else if (schemaData.SchemaUri == null)
            {
                // generate random schemaId
                schemaData.SchemaUri = SchemaRepositoryHelpers.GenerateRandomSchemaId();
            }

            if (!string.IsNullOrWhiteSpace(schemaData.SchemaUri.Fragment))
            {
                throw new InvalidOperationException($"Schema id cannot contain a uri fragment.");
            }

            AddSchema(schemaData);

            // Resolve vocabulary context from meta-schema if not already set
            ResolveVocabularyContext(schemaData);

            WalkElement(schemaData.Schema, schemaData.SchemaUri, schemaData.DraftVersion);
            return true;
        }

        /// <summary>
        /// Resolves vocabulary settings from the meta-schema referenced by $schema.
        /// Also resolves the underlying draft version for custom meta-schemas.
        /// </summary>
        private void ResolveVocabularyContext(SchemaMetadata schemaData)
        {
            // If this schema already has vocabulary defined (it's a meta-schema), skip
            if (schemaData.ActiveVocabularies != null)
                return;

            // Look up the meta-schema referenced by $schema
            if (string.IsNullOrEmpty(schemaData.DraftVersion))
                return;

            if (!Uri.TryCreate(schemaData.DraftVersion, UriKind.Absolute, out var metaSchemaUri))
                return;

            // Try to find the meta-schema in the repository
            if (_schemas.TryGetValue(metaSchemaUri, out var metaSchema))
            {
                // Inherit vocabulary settings from the meta-schema
                schemaData.ActiveVocabularies = metaSchema.ActiveVocabularies;
                schemaData.ActiveKeywords = metaSchema.ActiveKeywords;

                // Also inherit the underlying draft version from the meta-schema
                // This allows custom meta-schemas (that are based on standard drafts)
                // to work correctly with the validator factory routing
                if (!string.IsNullOrEmpty(metaSchema.DraftVersion))
                {
                    schemaData.DraftVersion = metaSchema.DraftVersion;
                }
            }
            // If meta-schema not found or has no vocabulary, use defaults (null = all keywords active)
        }

        private void AddSchema(SchemaMetadata schemaData)
        {
            if (!_schemas.TryAdd(schemaData.SchemaUri!, schemaData))
            {
                throw new InvalidOperationException($"Failed to register schema.");
            }
            else
            {
                schemaData.Order = _schemas.Count;
                // Create immutable snapshot for thread-safe iteration
                _sortedSchemas = _schemas.Values.OrderBy(s => s.Order).ToArray();
            }
        }

#pragma warning disable MA0051 // Method is too long
        private void WalkElement(JsonElement? schemaToRegister, Uri id, string? parentDraftVersion)
        {
#pragma warning restore MA0051
            if (schemaToRegister == null)
            {
                return;
            }

            var schema = schemaToRegister!.Value;
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            SchemaMetadata? schemaData = null;
            var newId = ExtractIdProperty(schema);

            // Draft 7 special case: $ref causes sibling keywords to be ignored, including $id.
            // Don't register sibling $id as a new base URI when $ref is present.
            bool isDraft7 = string.Equals(parentDraftVersion, "http://json-schema.org/draft-07/schema", StringComparison.Ordinal);
            bool hasRef = schema.TryGetProperty("$ref", out _);

            // In Draft 7 and earlier, $id starting with # is a "plain name fragment" (anchor),
            // not a base URI change. It should be treated like $anchor in 2019-09+.
            bool isPlainNameFragment = !string.IsNullOrWhiteSpace(newId) && newId.StartsWith('#');

            if (!string.IsNullOrWhiteSpace(newId) && !isPlainNameFragment && !(isDraft7 && hasRef))
            {
                if (!Uri.TryCreate(id, newId, out Uri? fullId))
                {
                    // failed to resolve id
                    throw new InvalidSchemaException($"Failed to resolve {newId} against {id}.");
                }
                id = fullId;

                if (_schemas.ContainsKey(id))
                {
                    if (!_schemas.TryGetValue(id, out schemaData))
                    {
                        throw new InvalidOperationException($"Failed to retrieve {id}.");
                    }
                }
                else
                {
                    // Inherit draft version from parent, or use configured default
                    var draftVersion = parentDraftVersion ?? _defaultDraftVersion;
                    schemaData = new SchemaMetadata(schema, draftVersion, id);
                    schemaData.HasRecursiveAnchor = HasRecursiveAnchorProperty(schema);
                    AddSchema(schemaData);
                }
            }

            // Handle plain name fragments from $id (e.g., $id: "#foo" in Draft 7)
            // These are treated as anchors
            if (isPlainNameFragment && newId != null && !(isDraft7 && hasRef))
            {
                if (schemaData == null && !_schemas.TryGetValue(id, out schemaData))
                {
                    throw new InvalidOperationException($"Failed to retrieve {id}.");
                }
                schemaData.Anchors.TryAdd(newId, schema);
            }

            // Draft 7: $ref ignores sibling $anchor as well
            var anchor = schema.GetAnchorProperty();
            if (!string.IsNullOrWhiteSpace(anchor) && !(isDraft7 && hasRef))
            {
                if (schemaData == null && !_schemas.TryGetValue(id, out schemaData))
                {
                    throw new InvalidOperationException($"Failed to retrieve {id}.");
                }
                schemaData.Anchors.TryAdd($"#{anchor}", schema);
            }

            var dynamicAnchor = schema.GetDynamicAnchorProperty();
            if (!string.IsNullOrWhiteSpace(dynamicAnchor))
            {
                if (schemaData == null && !_schemas.TryGetValue(id, out schemaData))
                {
                    throw new InvalidOperationException($"Failed to retrieve {id}.");
                }
                schemaData.DynamicAnchors.TryAdd($"#{dynamicAnchor}", schema);
            }

            // schema subobjects to walk through
            WalkSchemas(schema, "$defs", id, parentDraftVersion);
            WalkSchemas(schema, "definitions", id, parentDraftVersion); // Draft 7 and earlier

            WalkSchemas(schema, "properties", id, parentDraftVersion);
            WalkObject(schema, "additionalProperties", id, parentDraftVersion);
            WalkSchemas(schema, "patternProperties", id, parentDraftVersion);
            WalkObject(schema, "unevaluatedProperties", id, parentDraftVersion);
            WalkObject(schema, "propertyNames", id, parentDraftVersion);

            WalkArray(schema, "items", id, parentDraftVersion);
            WalkObject(schema, "items", id, parentDraftVersion);
            WalkArray(schema, "prefixItems", id, parentDraftVersion);
            WalkObject(schema, "unevaluatedItems", id, parentDraftVersion);
            WalkObject(schema, "additionalItems", id, parentDraftVersion);

            WalkObject(schema, "contains", id, parentDraftVersion);
            WalkSchemas(schema, "dependentSchemas", id, parentDraftVersion);
            WalkSchemas(schema, "dependencies", id, parentDraftVersion);

            WalkArray(schema, "allOf", id, parentDraftVersion);
            WalkArray(schema, "anyOf", id, parentDraftVersion);
            WalkArray(schema, "oneOf", id, parentDraftVersion);
            WalkObject(schema, "not", id, parentDraftVersion);

            WalkObject(schema, "if", id, parentDraftVersion);
            WalkObject(schema, "then", id, parentDraftVersion);
            WalkObject(schema, "else", id, parentDraftVersion);
        }

        private void WalkSchemas(JsonElement schema, string propertyName, Uri id, string? draftVersion)
        {
            var properties = schema.GetObjectProperty(propertyName);
            if (properties.ValueKind == JsonValueKind.Object)
            {
                foreach (var prp in properties.EnumerateObject())
                {
                    WalkElement(prp.Value, id, draftVersion);
                }
            }
        }

        private void WalkArray(JsonElement schema, string propertyName, Uri id, string? draftVersion)
        {
            var itemsAsArray = schema.GetArrayProperty(propertyName);
            if (itemsAsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsAsArray.EnumerateArray())
                {
                    WalkElement(item, id, draftVersion);
                }
            }
        }

        private void WalkObject(JsonElement schema, string propertyName, Uri id, string? draftVersion)
        {
            var itemsAsObject = schema.GetObjectProperty(propertyName);
            if (itemsAsObject.ValueKind == JsonValueKind.Object)
            {
                WalkElement(itemsAsObject, id, draftVersion);
            }
        }

        public SchemaMetadata GetSchema(Uri schemaUri, bool dynamicRef = false)
        {
            if (schemaUri == null) throw new ArgumentNullException(nameof(schemaUri));

            if (string.Equals(schemaUri.Fragment, "#", StringComparison.Ordinal))
            {
                schemaUri = new UriBuilder(schemaUri) { Fragment = string.Empty }.Uri;
            }

            if (!_schemas.TryGetValue(schemaUri, out var metadata))
            {
                throw new ArgumentException($"Schema with URI {schemaUri} not found.", nameof(schemaUri));
            }

            if (string.IsNullOrWhiteSpace(schemaUri.Fragment))
            {
                return new(metadata);
            }

            if (schemaUri.Fragment.StartsWith("#/", StringComparison.Ordinal))
            {
                string decodedFragment = Uri.UnescapeDataString(schemaUri.Fragment);
                var targetSchema = metadata.Schema.GetElementByJsonPointer(decodedFragment);

                // Check if the target has its own $id - if so, use that resource's context
                // This is important for $dynamicRef resolution which needs the correct DynamicAnchors
                var targetId = ExtractIdProperty(targetSchema);
                if (!string.IsNullOrEmpty(targetId))
                {
                    // Resolve the $id against the base URI (without fragment)
                    var baseUri = new UriBuilder(schemaUri) { Fragment = string.Empty }.Uri;
                    if (Uri.TryCreate(baseUri, targetId, out var resolvedId) &&
                        _schemas.TryGetValue(resolvedId, out var targetResource))
                    {
                        return new(targetResource);
                    }
                }

                SchemaMetadata innerSchemaData = new(metadata);
                innerSchemaData.Schema = targetSchema;
                innerSchemaData.SchemaUri = schemaUri;
                return innerSchemaData;
            }

            if (metadata.Anchors.TryGetValue(schemaUri.Fragment, out var anchoredSchema))
            {
                SchemaMetadata innerSchemaData = new(metadata);
                innerSchemaData.Schema = anchoredSchema;
                innerSchemaData.SchemaUri = schemaUri;
                return innerSchemaData;
            }

            if (metadata.DynamicAnchors.TryGetValue(schemaUri.Fragment, out var dynamicAnchoredSchema))
            {
                SchemaMetadata innerSchemaData = new(metadata);
                innerSchemaData.Schema = dynamicAnchoredSchema;
                innerSchemaData.SchemaUri = schemaUri;
                return innerSchemaData;
            }

            throw new ArgumentException($"Schema with URI {schemaUri} not found.", nameof(schemaUri));
        }

        public bool TryGetDynamicRef(string dynamicAnchor, out SchemaMetadata? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(dynamicAnchor) || !dynamicAnchor.StartsWith('#'))
            {
                return false;
            }

            // Take a local snapshot for thread-safe iteration
            var snapshot = _sortedSchemas;
            if (snapshot == null)
            {
                return false;
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                var schema = snapshot[i];
                if (schema.DynamicAnchors.TryGetValue(dynamicAnchor, out var anchoredSchema))
                {
                    result = new(schema)
                    {
                        Schema = anchoredSchema,
                    };
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the schema has $recursiveAnchor: true (Draft 2019-09 feature).
        /// </summary>
        private static bool HasRecursiveAnchorProperty(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!schema.TryGetProperty("$recursiveAnchor", out var recursiveAnchorElement))
            {
                return false;
            }

            return recursiveAnchorElement.ValueKind == JsonValueKind.True;
        }

        /// <summary>
        /// Extracts $id property value without draft-specific validation.
        /// This is needed because SchemaRepository works with all drafts.
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
