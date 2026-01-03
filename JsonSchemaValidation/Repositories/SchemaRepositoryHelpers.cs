using System.Text.Json;

namespace JsonSchemaValidation.Repositories
{
    public static class SchemaRepositoryHelpers
    {
        public static Uri GenerateRandomSchemaId()
        {
            var guid = Guid.NewGuid();
            return new Uri($"urn:schema:{guid}");
        }

        /// <summary>
        /// Creates a SchemaMetadata for a sub-schema, properly resolving any $id against the parent's base URI.
        /// </summary>
        public static SchemaMetadata CreateSubSchemaMetadata(SchemaMetadata parentSchemaData, JsonElement subSchema)
        {
            var subSchemaData = new SchemaMetadata(parentSchemaData)
            {
                Schema = subSchema
            };

            // Check if sub-schema has its own $id and resolve it against parent's base URI
            var subId = ExtractSchemaId(subSchema);
            if (!string.IsNullOrWhiteSpace(subId) && parentSchemaData.SchemaUri != null)
            {
                if (Uri.TryCreate(parentSchemaData.SchemaUri, subId, out Uri? resolvedUri))
                {
                    subSchemaData.SchemaUri = resolvedUri;
                }
            }

            return subSchemaData;
        }

        /// <summary>
        /// Extracts the $id value as a string (not resolved to URI yet).
        /// </summary>
        private static string? ExtractSchemaId(JsonElement schema)
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

        public static Uri? ExtractSchemaUri(JsonElement schema)
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

            if (!Uri.TryCreate(idValue, new UriCreationOptions(), out var result))
            {
                return null;
            }

            return result;
        }

        public static string? ExtractDraftVersion(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("$schema", out var draftVersionElement))
            {
                return null;
            }

            if (draftVersionElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return draftVersionElement.GetString();
        }
    }
}
