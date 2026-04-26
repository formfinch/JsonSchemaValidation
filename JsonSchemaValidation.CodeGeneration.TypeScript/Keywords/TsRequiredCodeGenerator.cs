// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for the "required" keyword.
/// </summary>
public sealed class TsRequiredCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "required";
    public int Priority => 95;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("required", out var req) &&
               req.ValueKind == JsonValueKind.Array;
    }

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("required", out var required) ||
            required.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var names = new List<string>();
        foreach (var item in required.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                names.Add(item.GetString()!);
            }
        }

        if (names.Count == 0) return string.Empty;

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        foreach (var name in names)
        {
            var literal = TsLiteral.String(name);
            sb.AppendLine($"  if (!Object.prototype.hasOwnProperty.call({v}, {literal})) return false;");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}
