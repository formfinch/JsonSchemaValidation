// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for "$dynamicRef" references when they can be resolved statically.
/// When $dynamicRef points to a local anchor (#anchorName), it resolves statically
/// just like $ref. Dynamic resolution only occurs when there's an outer schema
/// with a matching $dynamicAnchor in the dynamic scope.
/// </summary>
public sealed class DynamicRefCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "$dynamicRef";
    public int Priority => 199; // Run right after $ref

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!schema.TryGetProperty("$dynamicRef", out var refElement))
        {
            return false;
        }

        if (refElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var refValue = refElement.GetString();
        if (string.IsNullOrEmpty(refValue))
        {
            return false;
        }

        // Handle local references (both anchor #name and JSON pointer #/path)
        // These can be resolved statically when in the same schema resource
        return refValue.StartsWith('#');
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        // $dynamicRef is Draft 2020-12 only
        if (context.DetectedDraft != SchemaDraft.Draft202012)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("$dynamicRef", out var refElement))
        {
            return string.Empty;
        }

        var refValue = refElement.GetString();
        if (string.IsNullOrEmpty(refValue))
        {
            return string.Empty;
        }

        // Only handle local references
        if (!refValue.StartsWith('#'))
        {
            return $"// $dynamicRef with external URI not supported in compiled mode: {refValue}";
        }

        // Handle JSON Pointer references (e.g., "#/$defs/foo")
        // These behave like $ref - resolve within current resource
        if (refValue.StartsWith("#/"))
        {
            return ResolveAsRef(context, refValue);
        }

        // Handle anchor references (e.g., "#anchorName")
        var anchorName = refValue[1..]; // Remove the leading #

        // Check if the current resource has a $dynamicAnchor with this name (bookend check)
        // This is required for dynamic resolution to be enabled
        JsonElement? localDynamicAnchor = null;
        if (context.ResourceRoot.HasValue)
        {
            localDynamicAnchor = FindDynamicAnchorInResource(anchorName, context.ResourceRoot.Value);
        }

        if (!localDynamicAnchor.HasValue)
        {
            // No bookend - behave like $ref, resolve locally (can use $anchor or $dynamicAnchor)
            return ResolveAsRef(context, refValue);
        }

        // Bookend exists - generate code that searches the dynamic scope at runtime.
        // Per JSON Schema 2020-12 section 8.2.3.2:
        // "the outermost schema resource in the dynamic scope that defines an identically named fragment"
        var localHash = context.GetSubschemaHash(localDynamicAnchor.Value);

        if (context.RequiresScopeTracking)
        {
            // Generate scope-aware resolution code
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// $dynamicRef: {refValue} (with dynamic scope resolution)");
            sb.AppendLine($"if ({context.ScopeVariable}.TryResolveDynamicAnchor(\"{anchorName}\", out var _dynValidator_{localHash[..8]}))");
            sb.AppendLine("{");
            sb.AppendLine($"    if (!_dynValidator_{localHash[..8]}!({context.ElementVariable}, {context.ScopeVariable})) return false;");
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.AppendLine($"    if (!{context.GenerateValidateCall(localHash)}) return false;");
            sb.Append("}");
            return sb.ToString();
        }
        else
        {
            // Legacy path: no scope tracking, use static resolution or _dynamicScopeRoot fallback
            // Check for outer $dynamicAnchors at ROOT level (depth 0) only
            if (context.FindOuterDynamicAnchor != null && context.ResourceDepth > 0)
            {
                var outerAnchor = context.FindOuterDynamicAnchor(anchorName, 1);
                if (outerAnchor.HasValue)
                {
                    var targetHash = context.GetSubschemaHash(outerAnchor.Value);
                    return $"// $dynamicRef: {refValue} (resolved to outer $dynamicAnchor at root)\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
                }
            }

            // Fall back to _dynamicScopeRoot at runtime
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// $dynamicRef: {refValue} (with runtime scope check - legacy)");
            sb.AppendLine("if (_dynamicScopeRoot != null)");
            sb.AppendLine("{");
            sb.AppendLine("    if (!_dynamicScopeRoot.IsValid(e)) return false;");
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.AppendLine($"    if (!{context.GenerateValidateCall(localHash)}) return false;");
            sb.Append("}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Resolve the reference like a regular $ref (within current resource).
    /// </summary>
    private static string ResolveAsRef(CodeGenerationContext context, string refValue)
    {
        JsonElement? targetSchema;
        if (context.ResourceRoot.HasValue)
        {
            targetSchema = context.ResolveLocalRefInResource(refValue, context.ResourceRoot.Value);
        }
        else
        {
            targetSchema = context.ResolveLocalRef(refValue);
        }

        if (!targetSchema.HasValue)
        {
            return $"// WARNING: Could not resolve $dynamicRef: {refValue}";
        }

        var targetHash = context.GetSubschemaHash(targetSchema.Value);
        return $"// $dynamicRef: {refValue} (resolved as $ref)\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
    }

    /// <summary>
    /// Find a $dynamicAnchor with the given name within a schema resource.
    /// Only searches for $dynamicAnchor (not $anchor).
    /// </summary>
    private static JsonElement? FindDynamicAnchorInResource(string anchorName, JsonElement resourceRoot)
    {
        return FindDynamicAnchorInSchema(anchorName, resourceRoot);
    }

    /// <summary>
    /// Recursively search for a $dynamicAnchor within a schema.
    /// </summary>
    private static JsonElement? FindDynamicAnchorInSchema(string anchorName, JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Check if this schema has the $dynamicAnchor
        if (schema.TryGetProperty("$dynamicAnchor", out var dynamicAnchor) &&
            dynamicAnchor.ValueKind == JsonValueKind.String &&
            dynamicAnchor.GetString() == anchorName)
        {
            return schema;
        }

        // Search in subschema-containing keywords
        string[] objectKeywords = ["additionalProperties", "additionalItems", "items", "contains", "not", "if", "then", "else", "propertyNames", "unevaluatedProperties", "unevaluatedItems", "contentSchema"];
        string[] objectOfKeywords = ["properties", "patternProperties", "dependentSchemas", "$defs", "definitions"];
        string[] arrayKeywords = ["allOf", "anyOf", "oneOf", "prefixItems"];

        foreach (var keyword in objectKeywords)
        {
            if (schema.TryGetProperty(keyword, out var subschema))
            {
                // Skip if this subschema has its own $id (different resource)
                if (subschema.ValueKind == JsonValueKind.Object && subschema.TryGetProperty("$id", out _))
                {
                    continue;
                }

                var result = FindDynamicAnchorInSchema(anchorName, subschema);
                if (result.HasValue) return result;
            }
        }

        foreach (var keyword in objectOfKeywords)
        {
            if (schema.TryGetProperty(keyword, out var container) && container.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in container.EnumerateObject())
                {
                    // Skip if this subschema has its own $id (different resource)
                    if (prop.Value.ValueKind == JsonValueKind.Object && prop.Value.TryGetProperty("$id", out _))
                    {
                        continue;
                    }

                    var result = FindDynamicAnchorInSchema(anchorName, prop.Value);
                    if (result.HasValue) return result;
                }
            }
        }

        foreach (var keyword in arrayKeywords)
        {
            if (schema.TryGetProperty(keyword, out var array) && array.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in array.EnumerateArray())
                {
                    // Skip if this subschema has its own $id (different resource)
                    if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("$id", out _))
                    {
                        continue;
                    }

                    var result = FindDynamicAnchorInSchema(anchorName, item);
                    if (result.HasValue) return result;
                }
            }
        }

        return null;
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
