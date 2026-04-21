// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for the "allOf" keyword.
/// </summary>
public sealed class JsAllOfCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "allOf";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("allOf", out var a) &&
        a.ValueKind == JsonValueKind.Array;

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("allOf", out var allOf) ||
            allOf.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }
        var sb = new StringBuilder();
        foreach (var sub in allOf.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(sub);
            sb.AppendLine($"if (!{context.GenerateValidateCall(hash)}) return false;");
        }
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates JavaScript code for the "anyOf" keyword.
/// </summary>
public sealed class JsAnyOfCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "anyOf";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("anyOf", out var a) &&
        a.ValueKind == JsonValueKind.Array;

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("anyOf", out var anyOf) ||
            anyOf.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }
        var checks = new List<string>();
        foreach (var sub in anyOf.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(sub);
            checks.Add(context.GenerateValidateCall(hash));
        }
        if (checks.Count == 0) return string.Empty;
        return $"if (!({string.Join(" || ", checks)})) return false;";
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates JavaScript code for the "oneOf" keyword.
/// </summary>
public sealed class JsOneOfCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "oneOf";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("oneOf", out var a) &&
        a.ValueKind == JsonValueKind.Array;

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("oneOf", out var oneOf) ||
            oneOf.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  let _matches = 0;");
        foreach (var sub in oneOf.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(sub);
            sb.AppendLine($"  if ({context.GenerateValidateCall(hash)}) _matches++;");
            sb.AppendLine("  if (_matches > 1) return false;");
        }
        sb.AppendLine("  if (_matches !== 1) return false;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates JavaScript code for the "not" keyword.
/// </summary>
public sealed class JsNotCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "not";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("not", out _);

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("not", out var notElem))
        {
            return string.Empty;
        }
        var hash = context.GetSubschemaHash(notElem);
        return $"if ({context.GenerateValidateCall(hash)}) return false;";
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}
