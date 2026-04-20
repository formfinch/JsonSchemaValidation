// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;

/// <summary>
/// Gates the JS target to features actually supported by the MVP emitter.
/// The shared analyzer under-reports JS-specific limitations, so the gate
/// walks the schema and rejects deferred features before emission begins.
/// Returning early avoids emitting code that silently diverges from C# behavior.
/// </summary>
public static class JsCapabilityGate
{
    /// <summary>
    /// Keywords whose MVP JS emitter coverage is deferred to follow-up issues.
    /// Present in any subschema = reject.
    /// </summary>
    private static readonly HashSet<string> DeferredKeywords = new(StringComparer.Ordinal)
    {
        "unevaluatedProperties",
        "unevaluatedItems",
        "$dynamicRef",
        "$dynamicAnchor",
        "$recursiveRef",
        "$recursiveAnchor",
    };

    /// <summary>
    /// Drafts the MVP JS emitter supports. Others are rejected explicitly rather
    /// than silently producing Draft-2020-12-shaped output.
    /// </summary>
    private static readonly HashSet<SchemaDraft> SupportedDrafts = new()
    {
        SchemaDraft.Draft4,
        SchemaDraft.Draft202012,
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

        var rejection = Walk(root);
        return rejection;
    }

    private static string? Walk(JsonElement node)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
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

                    var childRejection = Walk(prop.Value);
                    if (childRejection != null)
                    {
                        return childRejection;
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in node.EnumerateArray())
                {
                    var childRejection = Walk(item);
                    if (childRejection != null)
                    {
                        return childRejection;
                    }
                }
                break;
        }

        return null;
    }

    /// <summary>
    /// A $ref is "external" (from the MVP's perspective) if it targets a URI
    /// rather than a pure fragment within the current document.
    /// Refs beginning with '#' are treated as local JSON Pointer refs.
    /// </summary>
    private static bool IsExternalRef(string refValue)
    {
        return !refValue.StartsWith('#');
    }
}
