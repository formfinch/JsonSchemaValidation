// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;

/// <summary>
/// Walks a schema using the JS target's own notion of applicator keywords
/// (the same sets the capability gate uses), producing:
///
/// 1. The set of subschema hashes that are actually reachable from the root
///    via JS-emittable keywords. Subschemas under annotation-only keywords
///    like contentSchema, default, examples are excluded so they do not get
///    turned into dead — and sometimes invalid — validator functions.
///
/// 2. A resource-root identity per hash, so we can detect when two text-
///    identical ref-containing subschemas appear under different resource
///    roots. The shared SubschemaExtractor deduplicates by content hash and
///    keeps only the first resource root, which would make local-ref
///    resolution resolve against the wrong resource at the second call site.
///    The safest MVP behavior is to detect that case and reject the schema
///    rather than silently validate against the wrong target.
/// </summary>
internal static class JsSchemaReachability
{
    // Keyword-shape sets kept in sync with JsCapabilityGate. Duplicated here
    // rather than shared to avoid accidental gate-mutation coupling; these
    // drive emission, the gate drives rejection.
    private static readonly Dictionary<SchemaDraft, HashSet<string>> SchemaValuedPerDraft = new()
    {
        [SchemaDraft.Draft4] = new(StringComparer.Ordinal)
        {
            "not",
            "additionalProperties",
            "additionalItems",
        },
        [SchemaDraft.Draft202012] = new(StringComparer.Ordinal)
        {
            "not",
            "if", "then", "else",
            "contains",
            "additionalProperties",
            "propertyNames",
            "unevaluatedProperties",
            "unevaluatedItems",
        },
    };

    private static readonly Dictionary<SchemaDraft, HashSet<string>> SchemaArrayPerDraft = new()
    {
        [SchemaDraft.Draft4] = new(StringComparer.Ordinal)
        {
            "allOf", "anyOf", "oneOf",
        },
        [SchemaDraft.Draft202012] = new(StringComparer.Ordinal)
        {
            "allOf", "anyOf", "oneOf",
            "prefixItems",
        },
    };

    private static readonly Dictionary<SchemaDraft, HashSet<string>> SchemaMapPerDraft = new()
    {
        [SchemaDraft.Draft4] = new(StringComparer.Ordinal)
        {
            "properties",
            "patternProperties",
            "definitions",
        },
        [SchemaDraft.Draft202012] = new(StringComparer.Ordinal)
        {
            "properties",
            "patternProperties",
            "$defs",
            "definitions", // legacy keyword, still walked by SubschemaExtractor
            "dependentSchemas",
        },
    };

    public sealed record Result(
        HashSet<string> ReachableHashes,
        string? Rejection);

    public static Result Analyze(
        JsonElement root,
        SchemaDraft draft,
        IReadOnlyDictionary<string, SubschemaInfo> uniqueSchemas)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        // Identity map: hash -> set of resource-root hashes under which this
        // subschema was seen. A hash that shows up under more than one resource
        // root AND contains a local $ref is the ambiguous case we reject.
        var resourceRootsByHash = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        Walk(root, currentResourceRootHash: SchemaHasher.ComputeHash(root),
            draft, reachable, resourceRootsByHash, uniqueSchemas);

        // Second pass: flag ambiguous-resource ref-containing subschemas.
        foreach (var (hash, resourceRoots) in resourceRootsByHash)
        {
            if (resourceRoots.Count <= 1) continue;
            if (!uniqueSchemas.TryGetValue(hash, out var info)) continue;
            if (info.Schema.ValueKind == JsonValueKind.Object &&
                info.Schema.TryGetProperty("$ref", out var refElem) &&
                refElem.ValueKind == JsonValueKind.String)
            {
                return new Result(reachable,
                    "JS target MVP cannot safely compile a schema where an identical $ref-containing " +
                    "subschema appears under multiple nested $id resources: the shared analyzer " +
                    "collapses them by content hash, so the emitted validator would resolve the ref " +
                    "against the wrong resource. Differentiate the subschemas (e.g., include the $id " +
                    "in the containing object) or split the schema so each resource's refs are unique.");
            }
        }

        return new Result(reachable, null);
    }

    private static void Walk(
        JsonElement node,
        string currentResourceRootHash,
        SchemaDraft draft,
        HashSet<string> reachable,
        Dictionary<string, HashSet<string>> resourceRootsByHash,
        IReadOnlyDictionary<string, SubschemaInfo> uniqueSchemas)
    {
        if (node.ValueKind == JsonValueKind.True || node.ValueKind == JsonValueKind.False)
        {
            var boolHash = SchemaHasher.ComputeHash(node);
            reachable.Add(boolHash);
            RecordResourceRoot(resourceRootsByHash, boolHash, currentResourceRootHash);
            return;
        }
        if (node.ValueKind != JsonValueKind.Object) return;

        var hash = SchemaHasher.ComputeHash(node);
        // Early-return on revisit to break ref cycles safely.
        if (!reachable.Add(hash))
        {
            RecordResourceRoot(resourceRootsByHash, hash, currentResourceRootHash);
            return;
        }
        RecordResourceRoot(resourceRootsByHash, hash, currentResourceRootHash);

        // Follow local $ref to mark the referenced subschema reachable. Without
        // this step a ref that targets a subschema under a non-applicator
        // keyword (extension keywords, annotations) would never have a
        // validate_<hash> function emitted, and the generated JS would raise
        // ReferenceError at runtime. External refs are rejected by the gate.
        if (node.TryGetProperty("$ref", out var refElem) &&
            refElem.ValueKind == JsonValueKind.String)
        {
            var refValue = refElem.GetString();
            if (!string.IsNullOrEmpty(refValue) && refValue.StartsWith('#'))
            {
                // Any collected subschema whose JsonPointerPath matches the ref
                // is considered the target. If the target's subschema is in
                // uniqueSchemas, walk it so it and its dependencies get marked.
                var target = ResolveLocalRef(refValue, uniqueSchemas);
                if (target.HasValue)
                {
                    Walk(target.Value, currentResourceRootHash, draft, reachable, resourceRootsByHash, uniqueSchemas);
                }
            }
        }

        // An $id on this object opens a new resource boundary.
        var nextResourceRootHash = DetectResourceRootHash(node, currentResourceRootHash, draft);

        var schemaValued = SchemaValuedPerDraft[draft];
        var schemaArray = SchemaArrayPerDraft[draft];
        var schemaMap = SchemaMapPerDraft[draft];

        foreach (var prop in node.EnumerateObject())
        {
            // items: shape depends on draft.
            if (prop.Name == "items")
            {
                if (draft == SchemaDraft.Draft4 && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        Walk(item, nextResourceRootHash, draft, reachable, resourceRootsByHash, uniqueSchemas);
                    }
                }
                else
                {
                    Walk(prop.Value, nextResourceRootHash, draft, reachable, resourceRootsByHash, uniqueSchemas);
                }
                continue;
            }

            // Draft 4 dependencies: schema-valued entries only.
            if (prop.Name == "dependencies" && draft == SchemaDraft.Draft4 &&
                prop.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var dep in prop.Value.EnumerateObject())
                {
                    if (dep.Value.ValueKind == JsonValueKind.Object ||
                        dep.Value.ValueKind == JsonValueKind.True ||
                        dep.Value.ValueKind == JsonValueKind.False)
                    {
                        Walk(dep.Value, nextResourceRootHash, draft, reachable, resourceRootsByHash, uniqueSchemas);
                    }
                }
                continue;
            }

            if (schemaValued.Contains(prop.Name))
            {
                Walk(prop.Value, nextResourceRootHash, draft, reachable, resourceRootsByHash, uniqueSchemas);
            }
            else if (schemaArray.Contains(prop.Name) && prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.Value.EnumerateArray())
                {
                    Walk(item, nextResourceRootHash, draft, reachable, resourceRootsByHash, uniqueSchemas);
                }
            }
            else if (schemaMap.Contains(prop.Name) && prop.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var entry in prop.Value.EnumerateObject())
                {
                    Walk(entry.Value, nextResourceRootHash, draft, reachable, resourceRootsByHash, uniqueSchemas);
                }
            }
            // Data / annotation keywords (contentSchema, default, examples, enum,
            // const, etc.) intentionally not walked — any subschema-shaped text
            // inside them is annotation, not a validation applicator.
        }
    }

    /// <summary>
    /// Resolves a local JSON Pointer ref ("#/foo/bar") against the set of
    /// already-extracted subschemas. Returns the target JsonElement when a
    /// subschema's JsonPointerPath matches the ref's fragment, else null.
    /// </summary>
    private static JsonElement? ResolveLocalRef(
        string refValue,
        IReadOnlyDictionary<string, SubschemaInfo> uniqueSchemas)
    {
        if (string.IsNullOrEmpty(refValue) || !refValue.StartsWith('#')) return null;
        var fragment = refValue.Substring(1); // strip leading '#'; empty means root
        foreach (var info in uniqueSchemas.Values)
        {
            var path = info.JsonPointerPath ?? string.Empty;
            if (path == fragment)
            {
                return info.Schema;
            }
        }
        return null;
    }

    private static string DetectResourceRootHash(JsonElement node, string currentRootHash, SchemaDraft draft)
    {
        // Nested $id (Draft 2020-12) or id (Draft 4) opens a new resource.
        var idKey = draft == SchemaDraft.Draft4 ? "id" : "$id";
        if (node.TryGetProperty(idKey, out var idElem) && idElem.ValueKind == JsonValueKind.String)
        {
            return SchemaHasher.ComputeHash(node);
        }
        return currentRootHash;
    }

    private static void RecordResourceRoot(
        Dictionary<string, HashSet<string>> map,
        string subschemaHash,
        string resourceRootHash)
    {
        if (!map.TryGetValue(subschemaHash, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            map[subschemaHash] = set;
        }
        set.Add(resourceRootHash);
    }
}
