// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for local "$ref" references.
/// External refs are rejected by JsCapabilityGate before emission begins, so this
/// generator only handles fragment refs (starting with '#') targeting the current
/// document. Resolved via the shared SubschemaExtractor and dispatched to the
/// target subschema's validate function.
/// </summary>
public sealed class JsRefCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "$ref";
    public int Priority => 200;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return false;
        if (!schema.TryGetProperty("$ref", out var r)) return false;
        return r.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(r.GetString());
    }

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("$ref", out var refElem)) return string.Empty;
        var refValue = refElem.GetString();
        if (string.IsNullOrEmpty(refValue)) return string.Empty;

        // Gate rejects non-'#' refs, so we only see local refs here.
        // Resolve against the current resource root (the nearest ancestor with $id),
        // falling back to document-root resolution only when no resource is in scope.
        var target = context.ResourceRoot.HasValue
            ? context.ResolveLocalRefInResource(refValue, context.ResourceRoot.Value)
            : context.ResolveLocalRef(refValue);
        if (!target.HasValue)
        {
            return $"// WARNING: Could not resolve $ref: {refValue}\nreturn false;";
        }
        var hash = context.GetSubschemaHash(target.Value);
        return $"if (!{context.GenerateValidateCall(hash)}) return false;";
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}
