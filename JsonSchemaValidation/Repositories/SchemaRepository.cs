using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Draft202012.Keywords.Format;
using JsonSchemaValidation.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Repositories
{
    public class SchemaRepository : ISchemaRepository
    {
        private readonly ConcurrentDictionary<Uri, SchemaMetadata> _schemas = new();
        private readonly SchemaValidationOptions _options;

        public SchemaRepository(SchemaValidationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// Try to add the schema to the repository. Returns false when the schema is already registered.
        public bool TryAddSchema(SchemaMetadata schemaData, out SchemaMetadata? result)
        {
            if (schemaData == null) throw new ArgumentNullException(nameof(schemaData));

            result = new SchemaMetadata(schemaData);

            if(!InitSchemaUri(result))
            {
                result = null;
                return false;
            }

            if(!InitDraftVersion(result))
            {
                result = null;
                return false;
            }

            if (!_schemas.TryAdd(result.SchemaUri!, result))
            {
                result = null;
                return false;
            }

            AddAnchorSchema(result, result.Schema);
            AddDefsSchemas(result);
            return true;
        }

        public SchemaMetadata GetSchema(Uri schemaUri)
        {
            if (schemaUri == null) throw new ArgumentNullException(nameof(schemaUri));

            if (!_schemas.TryGetValue(schemaUri, out var metadata))
            {
                throw new ArgumentException($"Schema with URI {schemaUri} not found.");
            }

            if (string.IsNullOrWhiteSpace(schemaUri.Fragment))
            {
                return metadata;
            }

            if(metadata.Anchors.ContainsKey(schemaUri.Fragment))
            {
                SchemaMetadata anchorSchemaData = new(metadata);
                anchorSchemaData.Schema = metadata.Anchors[schemaUri.Fragment];
                anchorSchemaData.SchemaUri = schemaUri;
                return anchorSchemaData;
            }

            if (schemaUri.Fragment.StartsWith("#/"))
            {
                SchemaMetadata innerSchemaData = new(metadata);
                innerSchemaData.Schema = metadata.Schema.GetElementByJsonPointer(schemaUri.Fragment);
                innerSchemaData.SchemaUri = schemaUri;
                return innerSchemaData;
            }

            throw new ArgumentException($"Schema with URI {schemaUri} not found.");
        }

        private void AddDefsSchemas(SchemaMetadata schemaData)
        {
            var defsElement = ExtractDefsElement(schemaData);
            if (defsElement == null)
            {
                return;
            }

            foreach (var prp in defsElement.Value.EnumerateObject())
            {
                AddIdSchema(schemaData, prp.Value);
                AddAnchorSchema(schemaData, prp.Value);
            }
        }

        private void AddIdSchema(SchemaMetadata schemaData, JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (!schema.TryGetProperty("$id", out var idElement))
            {
                return;
            }

            // Standard behavior is obvious misformatting is ignored
            if (idElement.ValueKind != JsonValueKind.String)
            {
                return;
            }

            var uriValidation = new UriValidationLogic(canBeRelative: true);
            var idText = idElement.ToString();
            var isValid = uriValidation.IsValidUri(idText);
            if (!isValid)
            {
                throw new InvalidSchemaException("The '$id' keyword must be a string representing a valid URI - reference.");
            }

            var rxPattern = new Regex($"^[^#]*#?$");
            if (!rxPattern.IsMatch(idText))
            {
                throw new InvalidSchemaException("The '$id' keyword cannot contain fragments. To use fragments, refer to the '$anchor' keyword.");
            }

            if (Uri.TryCreate(schemaData.SchemaUri, idText, out Uri? fullId))
            {
                var subSchemaData = new SchemaMetadata(schema, schemaData.DraftVersion, fullId);
                subSchemaData.UseSchemaUriForRegistration = true;
                TryAddSchema(subSchemaData, out _);
            }
        }

        private static void AddAnchorSchema(SchemaMetadata schemaData, JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (!schema.TryGetProperty("$anchor", out var anchorElement))
            {
                return;
            }

            // Standard behavior is obvious misformatting is ignored
            if (anchorElement.ValueKind != JsonValueKind.String)
            {
                return;
            }

            var anchorText = anchorElement.ToString();
            var rxPattern = new Regex($"^[A-Za-z_][-A-Za-z0-9._]*$");
            if (!rxPattern.IsMatch(anchorText))
            {
                throw new InvalidSchemaException("The '$anchor' keyword should be a short text value describing the current URI context.");
            }

            schemaData.Anchors.TryAdd($"#{anchorText}", schema);
        }

        private static JsonElement? ExtractDefsElement(SchemaMetadata schemaData)
        {
            if (schemaData?.Schema == null)
            {
                return null;
            }

            var schema = schemaData.Schema;
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("$defs", out var defsElement))
            {
                return null;
            }

            if (defsElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return defsElement;
        }

        private bool InitDraftVersion(SchemaMetadata targetSchemaData)
        {
            var draftVersion = SchemaRepositoryHelpers.ExtractDraftVersion(targetSchemaData.Schema);
            if (draftVersion == null)
            {
                // use fallback if provided
                draftVersion = targetSchemaData.DraftVersion;
            }

            if (draftVersion == null)
            {
                // use default draft version from options if no $schema present and no fallback provided
                draftVersion = _options.DefaultDraftVersion;
            }

            targetSchemaData.DraftVersion = draftVersion;
            if (string.IsNullOrWhiteSpace(targetSchemaData.DraftVersion))
            {
                throw new InvalidOperationException(@$"Json Schema Draft version could not be determined.");
            }

            return true;
        }

        private bool InitSchemaUri(SchemaMetadata targetSchemaData)
        {
            var schemaUri = targetSchemaData.SchemaUri;
            if(!targetSchemaData.UseSchemaUriForRegistration)
            {
                // Sub-schemas have the Id already resolved and stored in SchemaUri.
                // The id element in subschemas still contains the unresolved value and must not be used for registration.
                var extractedUri = SchemaRepositoryHelpers.ExtractSchemaUri(targetSchemaData.Schema);
                if (extractedUri != null)
                {
                    schemaUri = extractedUri;
                }
            }

            if (schemaUri == null)
            {
                // generate random schemaId if no $id present and no fallback provided
                schemaUri = SchemaRepositoryHelpers.GenerateRandomSchemaId();
            }

            if(!string.IsNullOrWhiteSpace(schemaUri.Fragment))
            {
                throw new InvalidOperationException($"Schema id cannot contain a uri fragment.");
            }

            targetSchemaData.SchemaUri = schemaUri!;
            if (_schemas.ContainsKey(targetSchemaData.SchemaUri))
            {
                return false;
            }
            return true;
        }
    }
}
