// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;

/// <summary>
/// Gates the JS target to features actually supported by the MVP emitter.
/// Walks only through known schema-valued keywords so that annotation/data
/// keywords (default, examples, enum, const) aren't confused for subschemas.
/// Returns early with a descriptive message when a deferred feature is found.
/// </summary>
public static class JsCapabilityGate
{
    private static readonly HashSet<string> DeferredKeywords = new(StringComparer.Ordinal)
    {
        "unevaluatedProperties",
        "unevaluatedItems",
        "$dynamicRef",
        "$dynamicAnchor",
        "$recursiveRef",
        "$recursiveAnchor",
    };

    private static readonly HashSet<SchemaDraft> SupportedDrafts = new()
    {
        SchemaDraft.Draft4,
        SchemaDraft.Draft202012,
    };

    /// <summary>
    /// Keywords whose value is itself a schema (object or boolean).
    /// </summary>
    private static readonly HashSet<string> SchemaValuedKeywords = new(StringComparer.Ordinal)
    {
        "not",
        "if", "then", "else",
        "contains",
        "additionalProperties",
        "additionalItems",
        "propertyNames",
        "unevaluatedProperties",
        "unevaluatedItems",
        "contentSchema",
    };

    /// <summary>
    /// Keywords whose value is an array of schemas.
    /// </summary>
    private static readonly HashSet<string> SchemaArrayKeywords = new(StringComparer.Ordinal)
    {
        "allOf", "anyOf", "oneOf",
        "prefixItems",
    };

    /// <summary>
    /// Keywords whose value is a map of name/pattern to schema.
    /// </summary>
    private static readonly HashSet<string> SchemaMapKeywords = new(StringComparer.Ordinal)
    {
        "properties",
        "patternProperties",
        "$defs", "definitions",
        "dependentSchemas",
    };

    /// <summary>
    /// Returns a rejection message if the schema uses deferred features,
    /// or null if the schema is emit-safe for the MVP JS target.
    /// </summary>
    public static string? CheckSupported(JsonElement root, SchemaDraft detectedDraft)
    {
        if (!SupportedDrafts.Contains(detectedDraft))
        {
            return $"JS target MVP supports Draft 4 and Draft 2020-12 only; detected {detectedDraft}. " +
                   "Other drafts are tracked as follow-up work.";
        }
        return Walk(root);
    }

    private static string? Walk(JsonElement node)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                return WalkObject(node);
            case JsonValueKind.True:
            case JsonValueKind.False:
                // Boolean schemas are valid; nothing to inspect.
                return null;
            default:
                // Primitives cannot host schema keywords.
                return null;
        }
    }

    private static string? WalkObject(JsonElement node)
    {
        foreach (var prop in node.EnumerateObject())
        {
            if (DeferredKeywords.Contains(prop.Name))
            {
                return $"JS target MVP does not support '{prop.Name}'. " +
                       "Deferred to follow-up: unevaluated*, $dynamicRef/$dynamicAnchor, " +
                       "$recursiveRef/$recursiveAnchor.";
            }

            if (prop.Name == "$ref" && prop.Value.ValueKind == JsonValueKind.String)
            {
                var refValue = prop.Value.GetString();
                if (!string.IsNullOrEmpty(refValue) && IsExternalRef(refValue))
                {
                    return $"JS target MVP does not support external $ref '{refValue}'. " +
                           "Local refs (starting with '#') are supported; external refs are deferred.";
                }
            }

            // Special case for "items": can be a single schema (all drafts) or an
            // array of schemas (Draft 4 + 2019-09). Recurse into whichever shape it is.
            if (prop.Name == "items")
            {
                var rejection = prop.Value.ValueKind == JsonValueKind.Array
                    ? WalkSchemaArray(prop.Value)
                    : Walk(prop.Value);
                if (rejection != null) return rejection;
                continue;
            }

            // Draft 4-7 "dependencies": each value is either an array of property
            // names (data) or a schema. Only recurse into schema-shaped values.
            if (prop.Name == "dependencies" && prop.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var dep in prop.Value.EnumerateObject())
                {
                    if (dep.Value.ValueKind == JsonValueKind.Object ||
                        dep.Value.ValueKind == JsonValueKind.True ||
                        dep.Value.ValueKind == JsonValueKind.False)
                    {
                        var rejection = Walk(dep.Value);
                        if (rejection != null) return rejection;
                    }
                }
                continue;
            }

            if (SchemaValuedKeywords.Contains(prop.Name))
            {
                var rejection = Walk(prop.Value);
                if (rejection != null) return rejection;
            }
            else if (SchemaArrayKeywords.Contains(prop.Name) &&
                     prop.Value.ValueKind == JsonValueKind.Array)
            {
                var rejection = WalkSchemaArray(prop.Value);
                if (rejection != null) return rejection;
            }
            else if (SchemaMapKeywords.Contains(prop.Name) &&
                     prop.Value.ValueKind == JsonValueKind.Object)
            {
                var rejection = WalkSchemaMap(prop.Value);
                if (rejection != null) return rejection;
            }
            // All other property values (default, examples, enum, const, type,
            // required, pattern, numeric constraints, format, etc.) are data or
            // annotations and are intentionally not walked.
        }
        return null;
    }

    private static string? WalkSchemaArray(JsonElement array)
    {
        foreach (var item in array.EnumerateArray())
        {
            var rejection = Walk(item);
            if (rejection != null) return rejection;
        }
        return null;
    }

    private static string? WalkSchemaMap(JsonElement map)
    {
        foreach (var entry in map.EnumerateObject())
        {
            var rejection = Walk(entry.Value);
            if (rejection != null) return rejection;
        }
        return null;
    }

    /// <summary>
    /// A $ref is "external" from the MVP's perspective when it targets something
    /// other than a fragment within the current document (anything not starting with '#').
    /// </summary>
    private static bool IsExternalRef(string refValue)
    {
        return !refValue.StartsWith('#');
    }
}
