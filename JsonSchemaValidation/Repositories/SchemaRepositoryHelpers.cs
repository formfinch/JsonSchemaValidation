using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Repositories
{
    public class SchemaRepositoryHelpers
    {
        public static Uri GenerateRandomSchemaId()
        {
            var guid = Guid.NewGuid();
            return new Uri($"urn:schema:{guid}");
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

            return new Uri(idValue);
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
