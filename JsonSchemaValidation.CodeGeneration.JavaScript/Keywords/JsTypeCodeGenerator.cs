// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for the "type" keyword.
/// </summary>
public sealed class JsTypeCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "type";
    public int Priority => 100;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("type", out _);
    }

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("type", out var typeElement))
        {
            return string.Empty;
        }

        var v = context.ElementExpr;

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return RejectUnlessType(typeElement.GetString()!, v);
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            return GenerateMultiTypeCheck(typeElement, v);
        }

        return string.Empty;
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            yield break;
        }

        if (!context.CurrentSchema.TryGetProperty("type", out var typeElement))
        {
            yield break;
        }

        if (ReferencesIntegerType(typeElement))
        {
            yield return "isInteger";
        }
    }

    private static bool ReferencesIntegerType(JsonElement typeElement)
    {
        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString() == "integer";
        }
        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in typeElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && item.GetString() == "integer")
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static string RejectUnlessType(string type, string v)
    {
        // "number" rejects NaN/Infinity to match JSON numeric semantics —
        // JSON.parse can't produce them, but direct API callers can.
        return type switch
        {
            "string" => $"if (typeof {v} !== \"string\") return false;",
            "number" => $"if (typeof {v} !== \"number\" || !Number.isFinite({v})) return false;",
            "integer" => $"if (!isInteger({v})) return false;",
            "boolean" => $"if (typeof {v} !== \"boolean\") return false;",
            "null" => $"if ({v} !== null) return false;",
            "array" => $"if (!Array.isArray({v})) return false;",
            "object" => $"if (typeof {v} !== \"object\" || {v} === null || Array.isArray({v})) return false;",
            _ => string.Empty,
        };
    }

    private static string GenerateMultiTypeCheck(JsonElement typeArray, string v)
    {
        var conditions = new List<string>();
        foreach (var item in typeArray.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) continue;
            var cond = item.GetString() switch
            {
                "string" => $"typeof {v} === \"string\"",
                "number" => $"(typeof {v} === \"number\" && Number.isFinite({v}))",
                "integer" => $"isInteger({v})",
                "boolean" => $"typeof {v} === \"boolean\"",
                "null" => $"{v} === null",
                "array" => $"Array.isArray({v})",
                "object" => $"(typeof {v} === \"object\" && {v} !== null && !Array.isArray({v}))",
                _ => null,
            };
            if (cond != null) conditions.Add(cond);
        }

        if (conditions.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("if (!(");
        sb.Append(string.Join(" || ", conditions));
        sb.AppendLine(")) return false;");
        return sb.ToString();
    }
}
