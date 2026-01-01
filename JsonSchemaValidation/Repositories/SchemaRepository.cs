using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Draft202012;
using JsonSchemaValidation.Draft202012.Keywords.Logic;
using JsonSchemaValidation.Exceptions;
using System.Collections.Concurrent;
using System.Text.Json;

namespace JsonSchemaValidation.Repositories
{
    public class SchemaRepository : ISchemaRepository
    {
        private readonly ConcurrentDictionary<Uri, SchemaMetadata> _schemas = new();
        private IEnumerable<SchemaMetadata> _sortedSchemas = null;
        private readonly SchemaValidationOptions _options;
        private readonly VocabularyParser? _vocabularyParser;

        public SchemaRepository(SchemaValidationOptions options, VocabularyParser? vocabularyParser = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _vocabularyParser = vocabularyParser;
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

            // todo: default draft version
            schemaData.DraftVersion ??= "https://json-schema.org/draft/2020-12/schema";

            // Parse $vocabulary if present (for meta-schemas)
            if (_vocabularyParser != null)
            {
                var vocabResult = _vocabularyParser.ParseVocabulary(schema);
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

            WalkElement(schemaData.Schema, schemaData.SchemaUri);
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
                schemaData.Order = _schemas.Count();
                _sortedSchemas = _schemas.Values.OrderBy(s => s.Order);
            }
        }

        private void WalkElement(JsonElement? schemaToRegister, Uri id)
        {
            if (schemaToRegister == null)
            {
                return;
            }
            
            var schema = schemaToRegister!.Value;
            if(schema.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            SchemaMetadata? schemaData = null;
            var newId = schema.GetIdProperty();
            if (!string.IsNullOrWhiteSpace(newId))
            {
                if (!Uri.TryCreate(id, newId, out Uri? fullId))
                {
                    // failed to resolve id
                    throw new InvalidSchemaException($"Failed to resolve {newId} against {id}.");
                }
                id = fullId;

                if(_schemas.ContainsKey(id))
                {
                    if(!_schemas.TryGetValue(id, out schemaData))
                    {
                        throw new InvalidOperationException($"Failed to retrieve {id}.");
                    }
                }
                else
                { 
                    schemaData = new SchemaMetadata(schema, "https://json-schema.org/draft/2020-12/schema", id);
                    AddSchema(schemaData);
                }
            }

            var anchor = schema.GetAnchorProperty();
            if (!string.IsNullOrWhiteSpace(anchor))
            {
                if(schemaData == null && !_schemas.TryGetValue(id, out schemaData))
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
            WalkSchemas(schema, "$defs", id);

            WalkSchemas(schema, "properties", id);
            WalkObject(schema, "additionalProperties", id);
            WalkSchemas(schema, "patternProperties", id);
            WalkObject(schema, "unevaluatedProperties", id);
            WalkObject(schema, "propertyNames", id);

            WalkArray(schema, "items", id);
            WalkObject(schema, "items", id);
            WalkArray(schema, "prefixItems", id);
            WalkObject(schema, "unevaluatedItems", id);
            WalkObject(schema, "additionalItems", id);

            WalkObject(schema, "contains", id);
            WalkSchemas(schema, "dependentSchemas", id);
            WalkSchemas(schema, "dependencies", id);

            WalkArray(schema, "allOf", id);
            WalkArray(schema, "anyOf", id);
            WalkArray(schema, "oneOf", id);
            WalkObject(schema, "not", id);

            WalkObject(schema, "if", id);
            WalkObject(schema, "then", id);
            WalkObject(schema, "else", id);
        }

        private void WalkSchemas(JsonElement schema, string propertyName, Uri id)
        {
            var properties = schema.GetObjectProperty(propertyName);
            if (properties.ValueKind == JsonValueKind.Object)
            {
                foreach (var prp in properties.EnumerateObject())
                {
                    WalkElement(prp.Value, id);
                }
            }
        }

        private void WalkArray(JsonElement schema, string propertyName, Uri id)
        {
            var itemsAsArray = schema.GetArrayProperty(propertyName);
            if (itemsAsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsAsArray.EnumerateArray())
                {
                    WalkElement(item, id);
                }
            }
        }

        private void WalkObject(JsonElement schema, string propertyName, Uri id)
        {
            var itemsAsObject = schema.GetObjectProperty(propertyName);
            if (itemsAsObject.ValueKind == JsonValueKind.Object)
            {
                WalkElement(itemsAsObject, id);
            }
        }

        public SchemaMetadata GetSchema(Uri schemaUri, bool dynamicRef = false)
        {
            if (schemaUri == null) throw new ArgumentNullException(nameof(schemaUri));

            if (schemaUri.Fragment == "#")
            {
                schemaUri = new UriBuilder(schemaUri) { Fragment = string.Empty }.Uri;
            }

            if (!_schemas.TryGetValue(schemaUri, out var metadata))
            {
                throw new ArgumentException($"Schema with URI {schemaUri} not found.");
            }

            if (string.IsNullOrWhiteSpace(schemaUri.Fragment))
            {
                return new(metadata);
            }

            if (schemaUri.Fragment.StartsWith("#/"))
            {
                SchemaMetadata innerSchemaData = new(metadata);
                string decodedFragment = Uri.UnescapeDataString(schemaUri.Fragment);
                innerSchemaData.Schema = metadata.Schema.GetElementByJsonPointer(decodedFragment);
                innerSchemaData.SchemaUri = schemaUri;
                return innerSchemaData;
            }

            if(metadata.Anchors.TryGetValue(schemaUri.Fragment, out var anchoredSchema))
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

            throw new ArgumentException($"Schema with URI {schemaUri} not found.");
        }

        public bool TryGetDynamicRef(string dynamicAnchor, out SchemaMetadata? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(dynamicAnchor) || !dynamicAnchor.StartsWith("#"))
            {
                return false;
            }

            foreach (var schema in _sortedSchemas)
            {
                if (schema.DynamicAnchors.TryGetValue(dynamicAnchor, out var anchoredSchema))
                {
                    result = new(schema)
                    {
                        Schema = anchoredSchema
                    };
                    return true;
                }
            }
            return false;
        }
    }
}
