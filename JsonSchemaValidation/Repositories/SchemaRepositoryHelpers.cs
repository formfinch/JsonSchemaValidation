// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Repositories
{
    internal static class SchemaRepositoryHelpers
    {
        public static Uri GenerateRandomSchemaId()
        {
            var guid = Guid.NewGuid();
            return new Uri($"urn:schema:{guid.ToString()}");
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

            // In Draft 3-7, $ref causes sibling keywords to be ignored, including $id/id.
            // Don't apply sibling $id when $ref is present in these drafts.
            bool hasRef = subSchema.ValueKind == JsonValueKind.Object && subSchema.TryGetProperty("$ref", out _);

            if (hasRef && IsRefIgnoresSiblingsDraft(parentSchemaData.DraftVersion))
            {
                // In Draft 3-7, $ref ignores sibling $id, so don't change the base URI
                return subSchemaData;
            }

            // Check if sub-schema has its own $id and resolve it against parent's base URI
            var subId = ExtractSchemaId(subSchema);
            if (!string.IsNullOrWhiteSpace(subId)
                && parentSchemaData.SchemaUri != null
                && Uri.TryCreate(parentSchemaData.SchemaUri, subId, out Uri? resolvedUri))
            {
                subSchemaData.SchemaUri = resolvedUri;
            }

            return subSchemaData;
        }

        /// <summary>
        /// Checks if the given draft version is one where $ref causes sibling keywords to be ignored.
        /// This applies to Draft 3, 4, 6, and 7. In Draft 2019-09 and later, $ref is just another
        /// applicator and sibling keywords are processed.
        /// </summary>
        private static bool IsRefIgnoresSiblingsDraft(string? draftVersion)
        {
            if (string.IsNullOrEmpty(draftVersion))
                return false;

            // Draft 3, 4, 6, 7 all have $ref ignoring siblings
            return draftVersion.Contains("draft-03", StringComparison.OrdinalIgnoreCase) ||
                   draftVersion.Contains("draft-04", StringComparison.OrdinalIgnoreCase) ||
                   draftVersion.Contains("draft-06", StringComparison.OrdinalIgnoreCase) ||
                   draftVersion.Contains("draft-07", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Extracts the $id value as a string (not resolved to URI yet).
        /// Supports both $id (Draft 6+) and id (Draft 4).
        /// </summary>
        private static string? ExtractSchemaId(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Try $id first (Draft 6+)
            if (schema.TryGetProperty("$id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
            {
                return idElement.GetString();
            }

            // Fall back to id (Draft 4)
            if (schema.TryGetProperty("id", out idElement) && idElement.ValueKind == JsonValueKind.String)
            {
                return idElement.GetString();
            }

            return null;
        }

        /// <summary>
        /// Extracts the schema URI from $id (Draft 6+) or id (Draft 4).
        /// </summary>
        public static Uri? ExtractSchemaUri(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Try $id first (Draft 6+), then fall back to id (Draft 4)
            JsonElement idElement;
            bool hasId = (schema.TryGetProperty("$id", out idElement) && idElement.ValueKind == JsonValueKind.String)
                      || (schema.TryGetProperty("id", out idElement) && idElement.ValueKind == JsonValueKind.String);

            if (!hasId)
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

            // On Linux, /-prefixed paths like "/base" are parsed as file:///base.
            // JSON Schema URIs are never file:// URIs, so reject them.
            if (string.Equals(result.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
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
