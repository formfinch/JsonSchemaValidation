// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for the "enum" keyword.
/// Uses the runtime deepEquals helper for structural comparison.
/// </summary>
public sealed class TsEnumCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "enum";
    public int Priority => 80;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("enum", out var e) &&
               e.ValueKind == JsonValueKind.Array;
    }

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("enum", out var enumElem) ||
            enumElem.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var v = context.ElementExpr;
        var checks = new List<string>();
        foreach (var item in enumElem.EnumerateArray())
        {
            checks.Add($"deepEquals({v}, {TsLiteral.JSONAsExpression(item)})");
        }
        if (checks.Count == 0)
        {
            // Empty enum never matches.
            return "return false;";
        }

        var sb = new StringBuilder();
        sb.Append("if (!(");
        sb.Append(string.Join(" || ", checks));
        sb.AppendLine(")) return false;");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            yield break;
        }

        if (context.CurrentSchema.ValueKind == JsonValueKind.Object &&
            context.CurrentSchema.TryGetProperty("enum", out var e) &&
            e.ValueKind == JsonValueKind.Array &&
            e.GetArrayLength() > 0)
        {
            yield return "deepEquals";
        }
    }
}

/// <summary>
/// Generates TypeScript code for the "const" keyword.
/// </summary>
public sealed class TsConstCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "const";
    public int Priority => 80;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("const", out _);
    }

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            return string.Empty;
        }

        // const was introduced in Draft 6; for Draft 4 it is an unknown keyword
        // and must be ignored (annotation-only), not enforced.
        if (context.DetectedDraft < SchemaDraft.Draft6) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("const", out var constElem))
        {
            return string.Empty;
        }
        var v = context.ElementExpr;
        return $"if (!deepEquals({v}, {TsLiteral.JSONAsExpression(constElem)})) return false;";
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled) yield break;
        if (context.DetectedDraft < SchemaDraft.Draft6) yield break;
        yield return "deepEquals";
    }
}
