using JsonSchemaValidation.Abstractions;
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
        private readonly ConcurrentDictionary<Uri, JsonDocument> _schemas = new();

        public void AddSchema(JsonDocument schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            Uri schemaUri = ExtractSchemaUri(schema);
            _schemas[schemaUri] = schema;
        }

        public JsonDocument GetSchema(Uri schemaUri)
        {
            if (_schemas.TryGetValue(schemaUri, out var schema))
                return schema;

            throw new ArgumentException($"Schema with URI {schemaUri} not found.");
        }

        private static Uri ExtractSchemaUri(JsonDocument schema)
        {
            // The schemaUri should be stored under the "$id" property in the schema.
            if (schema.RootElement.TryGetProperty("$id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
            {
                string? idValue = idElement.GetString();
                if(!string.IsNullOrWhiteSpace(idValue))
                {
                    return new Uri(idValue);
                }
            }
            throw new ArgumentException("The provided schema does not contain a valid URI.");
        }
    }
}
