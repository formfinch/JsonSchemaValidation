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
        private readonly ConcurrentDictionary<Uri, JsonElement> _schemas = new();

        public Uri AddSchema(JsonElement schema, Uri? fallbackUri = null)
        {
            if (schema.ValueKind == JsonValueKind.Undefined)
                throw new ArgumentNullException(nameof(schema));

            Uri schemaUri = ExtractSchemaUri(schema, fallbackUri);
            _schemas[schemaUri] = schema;
            return schemaUri;
        }

        public JsonElement GetSchema(Uri schemaUri)
        {
            if (_schemas.TryGetValue(schemaUri, out var schema))
                return schema;

            throw new ArgumentException($"Schema with URI {schemaUri} not found.");
        }

        private static Uri ExtractSchemaUri(JsonElement schema, Uri? fallbackUri)
        {
            // The schemaUri should be stored under the "$id" property in the schema.
            if (schema.ValueKind == JsonValueKind.Object 
                && schema.TryGetProperty("$id", out var idElement) 
                && idElement.ValueKind == JsonValueKind.String)
            {
                string? idValue = idElement.GetString();
                if (!string.IsNullOrWhiteSpace(idValue))
                {
                    return new Uri(idValue);
                }
            }

            if (fallbackUri != null)
                return fallbackUri;

            throw new ArgumentException("The provided schema does not contain a valid URI and no fallback URI was provided.");
        }
    }
}
