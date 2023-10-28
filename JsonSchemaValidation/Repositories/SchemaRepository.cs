using JsonSchemaValidation.Abstractions;
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

            return target;
        }

        public SchemaMetadata GetSchema(Uri schemaUri)
        {
            if (schemaUri == null) throw new ArgumentNullException(nameof(schemaUri));

            if (!_schemas.TryGetValue(schemaUri, out var metadata))
            {
                throw new ArgumentException($"Schema with URI {schemaUri} not found.");
            }
            return metadata;
        }

        private void InitDraftVersion(SchemaMetadata target)
        {
            var draftVersion = SchemaRepositoryHelpers.ExtractDraftVersion(target.Schema);
            if (draftVersion == null)
            {
                // use fallback if provided
                draftVersion = target.DraftVersion;
            }

            if (draftVersion == null)
            {
                // use default draft version from options if no $schema present and no fallback provided
                draftVersion = _options.DefaultDraftVersion;
            }

            target.DraftVersion = draftVersion;
            if (string.IsNullOrWhiteSpace(target.DraftVersion))
            {
                throw new InvalidOperationException(@$"Json Schema Draft version could not be determined.");
            }
        }

        private void InitSchemaUri(SchemaMetadata target)
        {
            var schemaUri = SchemaRepositoryHelpers.ExtractSchemaUri(target.Schema);
            if (schemaUri == null)
            {
                // use fallback if provided
                schemaUri = target.SchemaUri;
            }

            if (schemaUri == null)
            {
                // generate random schemaId if no $id present and no fallback provided
                schemaUri = SchemaRepositoryHelpers.GenerateRandomSchemaId();
            }

            target.SchemaUri = schemaUri!;
            if (_schemas.ContainsKey(target.SchemaUri))
            {
                throw new InvalidOperationException(@$"A schema with uri {target.SchemaUri} has already been registered.");
            }
        }
    }
}
