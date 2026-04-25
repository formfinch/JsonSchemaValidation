// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.TypeScript;

/// <summary>
/// Gates the TS target to features actually supported by the MVP emitter.
/// Walking is draft-aware: deferred-feature rejection and applicator recursion
/// only apply when the keyword actually exists in the detected draft, so a
/// Draft 4 schema using a post-Draft-4 name (e.g. unevaluatedProperties,
/// $dynamicRef, propertyNames) is correctly accepted as an unknown-keyword
/// annotation per spec, not rejected.
/// </summary>
public static class TsCapabilityGate
{
    /// <summary>
    /// Stable prefix every gate-rejection message starts with. Callers (tests,
    /// tooling) can pattern-match this constant to classify a codegen failure as
    /// "gate rejection" without parsing free-form wording.
    /// </summary>
    public const string RejectionPrefix = "TS target MVP";

    private static readonly HashSet<SchemaDraft> SupportedDrafts = new()
    {
        SchemaDraft.Draft4,
        SchemaDraft.Draft201909,
        SchemaDraft.Draft202012,
    };

    /// <summary>
    /// Per-draft deferred-keyword set: reject only when the keyword exists in
    /// the target draft. Empty for Draft 4 because none of these keywords were
    /// part of that draft.
    /// </summary>
    private static readonly Dictionary<SchemaDraft, HashSet<string>> DeferredPerDraft = new()
    {
        [SchemaDraft.Draft4] = new(StringComparer.Ordinal),
        [SchemaDraft.Draft201909] = new(StringComparer.Ordinal)
        {
            "$recursiveRef",
            "$recursiveAnchor",
            // $dynamicRef / $dynamicAnchor are core keywords in Draft 2019-09
            // but JsDynamicRefCodeGenerator only emits scope-aware dispatch
            // under Draft 2020-12; reject pre-emission to avoid silently
            // ignoring them. Remove when 2019-09 scope tracking lands.
            "$dynamicRef",
            "$dynamicAnchor",
        },
        [SchemaDraft.Draft202012] = new(StringComparer.Ordinal)
        {
            "$recursiveRef",
            "$recursiveAnchor",
        },
    };

    /// <summary>
    /// Per-draft keywords whose value is itself a schema (object or boolean).
    /// Recursion only happens for keywords defined in the current draft; others
    /// are treated as unknown annotations and not traversed.
    /// </summary>
    private static readonly Dictionary<SchemaDraft, HashSet<string>> SchemaValuedPerDraft = new()
    {
        [SchemaDraft.Draft4] = new(StringComparer.Ordinal)
        {
            "not",
            "additionalProperties",
            "additionalItems",
        },
        [SchemaDraft.Draft201909] = new(StringComparer.Ordinal)
        {
            "not",
            "if", "then", "else",
            "contains",
            "additionalProperties",
            "propertyNames",
            "unevaluatedProperties",
            "unevaluatedItems",
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
            // contentSchema is annotation-only in this codebase (and in the default
            // 2020-12 content vocabulary), so recursing into it would falsely reject
            // schemas that use deferred keywords inside content metadata.
        },
    };

    /// <summary>
    /// Per-draft keywords whose value is an array of schemas.
    /// </summary>
    private static readonly Dictionary<SchemaDraft, HashSet<string>> SchemaArrayPerDraft = new()
    {
        [SchemaDraft.Draft4] = new(StringComparer.Ordinal)
        {
            "allOf", "anyOf", "oneOf",
        },
        [SchemaDraft.Draft201909] = new(StringComparer.Ordinal)
        {
            "allOf", "anyOf", "oneOf",
        },
        [SchemaDraft.Draft202012] = new(StringComparer.Ordinal)
        {
            "allOf", "anyOf", "oneOf",
            "prefixItems",
        },
    };

    /// <summary>
    /// Per-draft keywords whose value is a map of name/pattern to schema.
    /// </summary>
    private static readonly Dictionary<SchemaDraft, HashSet<string>> SchemaMapPerDraft = new()
    {
        [SchemaDraft.Draft4] = new(StringComparer.Ordinal)
        {
            "properties",
            "patternProperties",
            "definitions",
        },
        [SchemaDraft.Draft201909] = new(StringComparer.Ordinal)
        {
            "properties",
            "patternProperties",
            "$defs",
            "definitions",
            "dependentSchemas",
        },
        [SchemaDraft.Draft202012] = new(StringComparer.Ordinal)
        {
            "properties",
            "patternProperties",
            "$defs",
            // definitions: not a standard 2020-12 keyword, but the shared
            // SubschemaExtractor still walks it and #/definitions/... refs
            // are legal JSON Pointers. Include so the gate matches emission
            // reachability and so extractor-collected subschemas get emitted.
            "definitions",
            "dependentSchemas",
        },
    };

    /// <summary>
    /// Returns a rejection message if the schema uses deferred features,
    /// or null if the schema is emit-safe for the MVP TS target.
    /// </summary>
    public static string? CheckSupported(JsonElement root, SchemaDraft detectedDraft)
    {
        if (!SupportedDrafts.Contains(detectedDraft))
        {
            return $"TS target MVP supports Draft 4, Draft 2019-09, and Draft 2020-12 only; detected {detectedDraft}. " +
                   "Other drafts are tracked as follow-up work.";
        }
        return Walk(root, detectedDraft);
    }

    private static string? Walk(JsonElement node, SchemaDraft draft)
    {
        return node.ValueKind == JsonValueKind.Object ? WalkObject(node, draft) : null;
    }

    private static string? WalkObject(JsonElement node, SchemaDraft draft)
    {
        var deferred = DeferredPerDraft[draft];
        var schemaValued = SchemaValuedPerDraft[draft];
        var schemaArray = SchemaArrayPerDraft[draft];
        var schemaMap = SchemaMapPerDraft[draft];

        // Draft 7 and earlier: $ref masks all sibling keywords at emission time
        // (see TsSchemaCodeGenerator.refMasksSiblings). Mirror that here so the
        // gate doesn't reject schemas based on keywords the emitter will ignore.
        // Match the emitter's "usable $ref" check — an empty or non-string $ref
        // doesn't actually mask siblings in the emitter, so we don't mask here
        // either.
        var refMasksSiblings = draft <= SchemaDraft.Draft7 &&
                               node.TryGetProperty("$ref", out var maskedRef) &&
                               maskedRef.ValueKind == JsonValueKind.String &&
                               !string.IsNullOrEmpty(maskedRef.GetString());

        foreach (var prop in node.EnumerateObject())
        {
            if (refMasksSiblings && prop.Name != "$ref")
            {
                continue;
            }
            if (deferred.Contains(prop.Name))
            {
                return $"TS target MVP does not support '{prop.Name}'. " +
                       $"Deferred to follow-up: support for '{prop.Name}' is not yet implemented.";
            }

            // Draft-specific "items": single schema in 2020-12; single or array in Draft 4.
            // Only traverse when items is a known keyword in the current draft (it is in both
            // MVP drafts, but the shape differs).
            if (prop.Name == "items")
            {
                if (draft == SchemaDraft.Draft4 && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    var rejection = WalkSchemaArray(prop.Value, draft);
                    if (rejection != null) return rejection;
                }
                else
                {
                    var rejection = Walk(prop.Value, draft);
                    if (rejection != null) return rejection;
                }
                continue;
            }

            // Draft 4 "dependencies": each value is either an array of property
            // names (data) or a schema. Only recurse into schema-shaped values.
            // In Draft 2020-12 "dependencies" is no longer a keyword; treat as
            // unknown annotation and do not recurse.
            if (prop.Name == "dependencies" && draft == SchemaDraft.Draft4 &&
                prop.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var dep in prop.Value.EnumerateObject())
                {
                    if (dep.Value.ValueKind == JsonValueKind.Object ||
                        dep.Value.ValueKind == JsonValueKind.True ||
                        dep.Value.ValueKind == JsonValueKind.False)
                    {
                        var rejection = Walk(dep.Value, draft);
                        if (rejection != null) return rejection;
                    }
                }
                continue;
            }

            if (schemaValued.Contains(prop.Name))
            {
                var rejection = Walk(prop.Value, draft);
                if (rejection != null) return rejection;
            }
            else if (schemaArray.Contains(prop.Name) &&
                     prop.Value.ValueKind == JsonValueKind.Array)
            {
                var rejection = WalkSchemaArray(prop.Value, draft);
                if (rejection != null) return rejection;
            }
            else if (schemaMap.Contains(prop.Name) &&
                     prop.Value.ValueKind == JsonValueKind.Object)
            {
                var rejection = WalkSchemaMap(prop.Value, draft);
                if (rejection != null) return rejection;
            }
            // All other property values (data, annotations, unknown-in-this-draft
            // keywords) are intentionally not walked.
        }
        return null;
    }

    private static string? WalkSchemaArray(JsonElement array, SchemaDraft draft)
    {
        foreach (var item in array.EnumerateArray())
        {
            var rejection = Walk(item, draft);
            if (rejection != null) return rejection;
        }
        return null;
    }

    private static string? WalkSchemaMap(JsonElement map, SchemaDraft draft)
    {
        foreach (var entry in map.EnumerateObject())
        {
            var rejection = Walk(entry.Value, draft);
            if (rejection != null) return rejection;
        }
        return null;
    }

}
