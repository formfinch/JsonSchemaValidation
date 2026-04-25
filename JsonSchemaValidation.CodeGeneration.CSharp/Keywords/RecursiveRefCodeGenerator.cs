// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for "$recursiveRef" references (Draft 2019-09 only).
/// $recursiveRef must always be "#" and references the root of the current resource.
/// Dynamic resolution occurs when there's an outer schema with $recursiveAnchor: true.
/// This was replaced by $dynamicRef/$dynamicAnchor in Draft 2020-12.
/// </summary>
public sealed class RecursiveRefCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "$recursiveRef";
    public int Priority => 199; // Run right after $ref

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!schema.TryGetProperty("$recursiveRef", out var refElement))
        {
            return false;
        }

        if (refElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var refValue = refElement.GetString();

        // $recursiveRef must always be "#"
        return refValue == "#";
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
    {
        // $recursiveRef is Draft 2019-09 only
        if (context.DetectedDraft != SchemaDraft.Draft201909)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("$recursiveRef", out var refElement))
        {
            return string.Empty;
        }

        var refValue = refElement.GetString();
        if (refValue != "#")
        {
            return $"// $recursiveRef must be \"#\", got: {refValue}";
        }

        // Check if the current resource root has $recursiveAnchor: true (bookend check)
        var hasLocalRecursiveAnchor = false;
        if (context.ResourceRoot.HasValue)
        {
            hasLocalRecursiveAnchor = HasRecursiveAnchor(context.ResourceRoot.Value);
        }

        if (!hasLocalRecursiveAnchor)
        {
            // No bookend - resolve to local root
            return ResolveToLocalRoot(context);
        }

        // Bookend exists - generate code that searches the dynamic scope at runtime.
        // Per JSON Schema 2019-09 spec:
        // "examining the dynamic scope for the outermost schema that also contains $recursiveAnchor: true"
        var localRootHash = context.ResourceRoot.HasValue
            ? context.GetSubschemaHash(context.ResourceRoot.Value)
            : context.CurrentHash;

        if (context.RequiresScopeTracking)
        {
            // Generate scope-aware resolution code
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("// $recursiveRef: # (with dynamic scope resolution)");
            sb.AppendLine($"if ({context.ScopeVariable}.TryResolveRecursiveAnchor(out var _recValidator_{localRootHash[..8]}))");
            sb.AppendLine("{");
            sb.AppendLine($"    if (!_recValidator_{localRootHash[..8]}!({context.ElementVariable}, {context.ScopeVariable}, {context.LocationVariable})) return false;");
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.AppendLine($"    if (!{context.GenerateValidateCall(localRootHash)}) return false;");
            sb.Append("}");
            return sb.ToString();
        }
        else
        {
            // Legacy path: no scope tracking, use _dynamicScopeRoot fallback
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("// $recursiveRef: # (with runtime scope check - legacy)");
            sb.AppendLine("if (_dynamicScopeRoot != null)");
            sb.AppendLine("{");
            sb.AppendLine("    if (!_dynamicScopeRoot.IsValid(e)) return false;");
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.AppendLine($"    if (!{context.GenerateValidateCall(localRootHash)}) return false;");
            sb.Append("}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Resolve $recursiveRef to the local resource root.
    /// </summary>
    private static string ResolveToLocalRoot(CSharpCodeGenerationContext context)
    {
        if (!context.ResourceRoot.HasValue)
        {
            return "// WARNING: Could not resolve $recursiveRef: no resource root";
        }

        var targetHash = context.GetSubschemaHash(context.ResourceRoot.Value);
        return $"// $recursiveRef: # (resolved to local root)\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
    }

    /// <summary>
    /// Check if the schema has $recursiveAnchor: true.
    /// </summary>
    private static bool HasRecursiveAnchor(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (schema.TryGetProperty("$recursiveAnchor", out var anchor) &&
            anchor.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        return false;
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CSharpCodeGenerationContext context)
    {
        return [];
    }
}
