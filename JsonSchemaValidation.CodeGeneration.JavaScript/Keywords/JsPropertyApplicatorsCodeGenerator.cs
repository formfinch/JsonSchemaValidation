// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for the "patternProperties" keyword.
/// </summary>
public sealed class JsPatternPropertiesCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "patternProperties";
    public int Priority => 45;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("patternProperties", out var p) &&
        p.ValueKind == JsonValueKind.Object;

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("patternProperties", out var patterns) ||
            patterns.ValueKind != JsonValueKind.Object ||
            patterns.EnumerateObject().FirstOrDefault().Value.ValueKind == JsonValueKind.Undefined)
        {
            // If there are no entries, skip.
        }
        if (!context.CurrentSchema.TryGetProperty("patternProperties", out var patternsElem) ||
            patternsElem.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        sb.AppendLine($"  for (const _k of Object.keys({v})) {{");
        foreach (var pattern in patternsElem.EnumerateObject())
        {
            var regex = JsLiteral.RegexLiteral(pattern.Name);
            var hash = context.GetSubschemaHash(pattern.Value);
            sb.AppendLine($"    if ({regex}.test(_k)) {{");
            sb.AppendLine($"      if (!{context.GenerateValidateCallForExpr(hash, $"{v}[_k]")}) return false;");
            sb.AppendLine("    }");
        }
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates JavaScript code for the "additionalProperties" keyword.
/// Handles the three forms: false (reject unlisted), schema (validate unlisted), true (no-op).
/// </summary>
public sealed class JsAdditionalPropertiesCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "additionalProperties";
    public int Priority => 20;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("additionalProperties", out _);

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("additionalProperties", out var addProps))
        {
            return string.Empty;
        }
        if (addProps.ValueKind == JsonValueKind.True)
        {
            return string.Empty;
        }

        var definedNames = CollectDefinedNames(context.CurrentSchema);
        var patternRegexes = CollectPatternRegexes(context.CurrentSchema);

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        sb.AppendLine($"  for (const _k of Object.keys({v})) {{");
        if (definedNames.Count > 0)
        {
            var conds = definedNames.Select(n => $"_k === {JsLiteral.String(n)}");
            sb.AppendLine($"    if ({string.Join(" || ", conds)}) continue;");
        }
        if (patternRegexes.Count > 0)
        {
            var conds = patternRegexes.Select(r => $"{r}.test(_k)");
            sb.AppendLine($"    if ({string.Join(" || ", conds)}) continue;");
        }
        if (addProps.ValueKind == JsonValueKind.False)
        {
            sb.AppendLine("    return false;");
        }
        else
        {
            var hash = context.GetSubschemaHash(addProps);
            sb.AppendLine($"    if (!{context.GenerateValidateCallForExpr(hash, $"{v}[_k]")}) return false;");
        }
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];

    private static List<string> CollectDefinedNames(JsonElement schema)
    {
        var names = new List<string>();
        if (schema.TryGetProperty("properties", out var props) &&
            props.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in props.EnumerateObject())
            {
                names.Add(p.Name);
            }
        }
        return names;
    }

    private static List<string> CollectPatternRegexes(JsonElement schema)
    {
        var regexes = new List<string>();
        if (schema.TryGetProperty("patternProperties", out var patterns) &&
            patterns.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in patterns.EnumerateObject())
            {
                regexes.Add(JsLiteral.RegexLiteral(p.Name));
            }
        }
        return regexes;
    }
}

/// <summary>
/// Generates JavaScript code for the "propertyNames" keyword (Draft 6+).
/// </summary>
public sealed class JsPropertyNamesCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "propertyNames";
    public int Priority => 60;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("propertyNames", out _);

    public string GenerateCode(JsCodeGenerationContext context)
    {
        // propertyNames was introduced in Draft 6; Draft 4 must ignore it.
        if (context.DetectedDraft < SchemaDraft.Draft6) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("propertyNames", out var pn))
        {
            return string.Empty;
        }
        var hash = context.GetSubschemaHash(pn);
        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        sb.AppendLine($"  for (const _k of Object.keys({v})) {{");
        sb.AppendLine($"    if (!{context.GenerateValidateCallForExpr(hash, "_k")}) return false;");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}
