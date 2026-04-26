// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for the "items" keyword.
/// Draft 2020-12: items is a single schema applying to indices >= prefixItems count.
/// Draft 4 (MVP's other supported draft): items is either a single schema or an
/// array (tuple validation). The array-form branch also matches Draft 2019-09
/// keyword semantics at the code level, but Draft 2019-09 is rejected by
/// TsCapabilityGate for the JS target.
/// </summary>
public sealed class TsItemsCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "items";
    public int Priority => 40;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("items", out _);

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("items", out var items))
        {
            return string.Empty;
        }
        return context.DetectedDraft == SchemaDraft.Draft202012
            ? GenerateDraft202012(context, items)
            : GenerateLegacy(context, items);
    }

    private static string GenerateDraft202012(TsCodeGenerationContext ctx, JsonElement items)
    {
        if (items.ValueKind == JsonValueKind.Array)
        {
            // Draft 2020-12 replaced the tuple form of items with prefixItems.
            // Silently no-opping would let an invalid schema compile too loosely,
            // so surface this as a generation failure (matches dynamic validator).
            throw new InvalidOperationException(
                "Schema uses array-form \"items\" under Draft 2020-12. " +
                "Use \"prefixItems\" for tuple validation in 2020-12; \"items\" must be a single schema.");
        }
        var v = ctx.ElementExpr;
        var prefixCount = 0;
        if (ctx.CurrentSchema.TryGetProperty("prefixItems", out var prefixItems) &&
            prefixItems.ValueKind == JsonValueKind.Array)
        {
            prefixCount = prefixItems.GetArrayLength();
        }

        var sb = new StringBuilder();
        sb.AppendLine($"if (Array.isArray({v})) {{");
        sb.AppendLine($"  for (let _i = {prefixCount}; _i < {v}.length; _i++) {{");
        sb.AppendLine($"    if (!{ctx.GenerateValidateCallForItem(items, $"{v}[_i]", "_i")}) return false;");
        sb.AppendLine("  }");
        if (ctx.RequiresItemAnnotations)
        {
            sb.AppendLine($"  {ctx.EvaluatedStateExpr}.setEvaluatedItemsUpTo({ctx.LocationExpr}, {v}.length);");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateLegacy(TsCodeGenerationContext ctx, JsonElement items)
    {
        var v = ctx.ElementExpr;
        if (items.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"if (Array.isArray({v})) {{");
            var idx = 0;
            foreach (var sub in items.EnumerateArray())
            {
                sb.AppendLine($"  if ({v}.length > {idx} && !{ctx.GenerateValidateCallForItem(sub, $"{v}[{idx}]", idx.ToString(System.Globalization.CultureInfo.InvariantCulture))}) return false;");
                idx++;
            }
            if (ctx.RequiresItemAnnotations)
            {
                sb.AppendLine($"  {ctx.EvaluatedStateExpr}.setEvaluatedItemsUpTo({ctx.LocationExpr}, Math.min({idx}, {v}.length));");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
        if (items.ValueKind == JsonValueKind.Object ||
            items.ValueKind == JsonValueKind.True ||
            items.ValueKind == JsonValueKind.False)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"if (Array.isArray({v})) {{");
            sb.AppendLine($"  for (let _i = 0; _i < {v}.length; _i++) {{");
            sb.AppendLine($"    if (!{ctx.GenerateValidateCallForItem(items, $"{v}[_i]", "_i")}) return false;");
            sb.AppendLine("  }");
            if (ctx.RequiresItemAnnotations)
            {
                sb.AppendLine($"  {ctx.EvaluatedStateExpr}.setEvaluatedItemsUpTo({ctx.LocationExpr}, {v}.length);");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
        return string.Empty;
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates TypeScript code for the "prefixItems" keyword (Draft 2020-12 only).
/// </summary>
public sealed class TsPrefixItemsCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "prefixItems";
    public int Priority => 45;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("prefixItems", out var a) &&
        a.ValueKind == JsonValueKind.Array;

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (context.DetectedDraft != SchemaDraft.Draft202012) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("prefixItems", out var prefix) ||
            prefix.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }
        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (Array.isArray({v})) {{");
        var idx = 0;
        foreach (var sub in prefix.EnumerateArray())
        {
            sb.AppendLine($"  if ({v}.length > {idx} && !{context.GenerateValidateCallForItem(sub, $"{v}[{idx}]", idx.ToString(System.Globalization.CultureInfo.InvariantCulture))}) return false;");
            idx++;
        }
        if (context.RequiresItemAnnotations)
        {
            sb.AppendLine($"  {context.EvaluatedStateExpr}.setEvaluatedItemsUpTo({context.LocationExpr}, Math.min({idx}, {v}.length));");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates TypeScript code for the "additionalItems" keyword (Draft 4 in the TypeScript
/// target; also applies to Draft 2019-09 semantically but that draft is rejected
/// by TsCapabilityGate, so this generator only fires for Draft 4 schemas here).
/// Removed in Draft 2020-12. Only meaningful when items is an array (tuple validation).
/// </summary>
public sealed class TsAdditionalItemsCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "additionalItems";
    public int Priority => 42;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("additionalItems", out _);

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (context.DetectedDraft == SchemaDraft.Draft202012) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("additionalItems", out var additional))
        {
            return string.Empty;
        }
        if (!context.CurrentSchema.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }
        var tupleLen = items.GetArrayLength();
        var v = context.ElementExpr;

        if (additional.ValueKind == JsonValueKind.False)
        {
            return $"if (Array.isArray({v}) && {v}.length > {tupleLen}) return false;";
        }
        if (additional.ValueKind == JsonValueKind.True)
        {
            return string.Empty;
        }
        if (additional.ValueKind != JsonValueKind.Object)
        {
            // Invalid schema: additionalItems must be object/boolean per spec.
            // SubschemaExtractor skips non-schema values, so emitting a
            // validate_<hash> call would be undefined at runtime.
            throw new InvalidOperationException(
                "Schema's \"additionalItems\" value must be an object or boolean; got " +
                $"{additional.ValueKind}.");
        }
        var sb = new StringBuilder();
        sb.AppendLine($"if (Array.isArray({v})) {{");
        sb.AppendLine($"  for (let _i = {tupleLen}; _i < {v}.length; _i++) {{");
        sb.AppendLine($"    if (!{context.GenerateValidateCallForItem(additional, $"{v}[_i]", "_i")}) return false;");
        sb.AppendLine("  }");
        if (context.RequiresItemAnnotations)
        {
            sb.AppendLine($"  {context.EvaluatedStateExpr}.setEvaluatedItemsUpTo({context.LocationExpr}, {v}.length);");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}
