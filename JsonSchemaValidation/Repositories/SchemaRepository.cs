using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        public SchemaMetadata AddSchema(SchemaMetadata schemaData)
        {
            if (schemaData == null) throw new ArgumentNullException(nameof(schemaData));

            var target = new SchemaMetadata(schemaData);

            InitSchemaUri(target);
            InitDraftVersion(target);

            if (!_schemas.TryAdd(target.SchemaUri!, target))
            {
                throw new InvalidOperationException(@$"Schema could not be registered.");
            }

            AddDefsSchemas(schemaData);

            return target;
        }

        private void AddDefsSchemas(SchemaMetadata schemaData)
        {
            var defsElement = ExtractDefsElement(schemaData);
            if(defsElement == null)
            {
                return;
            }

            foreach(var prp in defsElement.Value.EnumerateObject())
            {
                if(prp.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                Uri? id = ExtractId(prp.Value);
                if(id == null)
                {
                    continue;

                }

                if(Uri.TryCreate(schemaData.SchemaUri, id.ToString(), out Uri? newUri))
                {
                    var value = new SchemaMetadata(prp.Value, schemaData.DraftVersion, schemaData.SchemaUri);
                    if (!_schemas.TryAdd(newUri, value))
                    {
                        throw new InvalidOperationException(@$"Schema in def$ could not be registered.");
                    }
                }
            }
        }

        public static Uri? ExtractId(JsonElement schema)
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

            string? idValue = idElement.GetString();
            if (string.IsNullOrWhiteSpace(idValue))
            {
                return null;
            }

            return new Uri(idValue);
        }

        private JsonElement? ExtractDefsElement(SchemaMetadata schemaData)
        {
            if(schemaData?.Schema == null) 
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

            SchemaMetadata innerSchemaData = new(metadata);
            innerSchemaData.Schema = metadata.Schema.GetElementByJsonPointer(schemaUri.Fragment);
            innerSchemaData.SchemaUri = schemaUri;
            return innerSchemaData;
        }

        private void InitDraftVersion(SchemaMetadata targetSchemaData)
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
        }

        private void InitSchemaUri(SchemaMetadata targetSchemaData)
        {
            var schemaUri = SchemaRepositoryHelpers.ExtractSchemaUri(targetSchemaData.Schema);
            if (schemaUri == null)
            {
                // use fallback if provided
                schemaUri = targetSchemaData.SchemaUri;
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
                throw new InvalidOperationException(@$"A schema with uri {targetSchemaData.SchemaUri} has already been registered.");
            }
        }
    }
}
