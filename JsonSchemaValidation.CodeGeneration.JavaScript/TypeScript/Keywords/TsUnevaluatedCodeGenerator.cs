// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for the "unevaluatedProperties" keyword.
/// </summary>
public sealed class TsUnevaluatedPropertiesCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "unevaluatedProperties";
    public int Priority => -100;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("unevaluatedProperties", out _);

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (context.DetectedDraft < SchemaDraft.Draft201909) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("unevaluatedProperties", out var unevaluated))
        {
            return string.Empty;
        }

        var v = context.ElementExpr;
        var eval = context.EvaluatedStateExpr;
        var loc = context.LocationExpr;
        var sb = new StringBuilder();

        if (unevaluated.ValueKind == JsonValueKind.False)
        {
            sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
            sb.AppendLine($"  for (const _k of Object.keys({v})) {{");
            sb.AppendLine($"    if (!{eval}.isPropertyEvaluated({loc}, _k)) return false;");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        if (unevaluated.ValueKind == JsonValueKind.True)
        {
            sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
            sb.AppendLine($"  for (const _k of Object.keys({v})) {{");
            sb.AppendLine($"    {eval}.markPropertyEvaluated({loc}, _k);");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        if (unevaluated.ValueKind == JsonValueKind.Object)
        {
            var hash = context.GetSubschemaHash(unevaluated);
            sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
            sb.AppendLine($"  for (const _k of Object.keys({v})) {{");
            sb.AppendLine($"    if (!{eval}.isPropertyEvaluated({loc}, _k)) {{");
            sb.AppendLine($"      if (!{context.GenerateValidateCallForProperty(hash, $"{v}[_k]", "_k")}) return false;");
            sb.AppendLine($"      {eval}.markPropertyEvaluated({loc}, _k);");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        throw new InvalidOperationException(
            "Schema's \"unevaluatedProperties\" value must be an object or boolean; got " +
            $"{unevaluated.ValueKind}.");
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates TypeScript code for the "unevaluatedItems" keyword.
/// </summary>
public sealed class TsUnevaluatedItemsCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "unevaluatedItems";
    public int Priority => -100;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("unevaluatedItems", out _);

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (context.DetectedDraft < SchemaDraft.Draft201909) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("unevaluatedItems", out var unevaluated))
        {
            return string.Empty;
        }

        var v = context.ElementExpr;
        var eval = context.EvaluatedStateExpr;
        var loc = context.LocationExpr;
        var sb = new StringBuilder();

        if (unevaluated.ValueKind == JsonValueKind.False)
        {
            sb.AppendLine($"if (Array.isArray({v})) {{");
            sb.AppendLine($"  for (let _i = 0; _i < {v}.length; _i++) {{");
            sb.AppendLine($"    if (!{eval}.isItemEvaluated({loc}, _i)) return false;");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        if (unevaluated.ValueKind == JsonValueKind.True)
        {
            sb.AppendLine($"if (Array.isArray({v})) {{");
            sb.AppendLine($"  {eval}.setEvaluatedItemsUpTo({loc}, {v}.length);");
            sb.AppendLine("}");
            return sb.ToString();
        }

        if (unevaluated.ValueKind == JsonValueKind.Object)
        {
            var hash = context.GetSubschemaHash(unevaluated);
            sb.AppendLine($"if (Array.isArray({v})) {{");
            sb.AppendLine($"  for (let _i = 0; _i < {v}.length; _i++) {{");
            sb.AppendLine($"    if (!{eval}.isItemEvaluated({loc}, _i)) {{");
            sb.AppendLine($"      if (!{context.GenerateValidateCallForItem(hash, $"{v}[_i]", "_i")}) return false;");
            sb.AppendLine($"      {eval}.markItemEvaluated({loc}, _i);");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine($"  {eval}.setEvaluatedItemsUpTo({loc}, {v}.length);");
            sb.AppendLine("}");
            return sb.ToString();
        }

        throw new InvalidOperationException(
            "Schema's \"unevaluatedItems\" value must be an object or boolean; got " +
            $"{unevaluated.ValueKind}.");
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}
