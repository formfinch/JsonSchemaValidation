// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for the "allOf" keyword.
/// </summary>
public sealed class TsAllOfCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "allOf";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("allOf", out var a) &&
        a.ValueKind == JsonValueKind.Array;

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("allOf", out var allOf) ||
            allOf.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }
        if (context.RequiresAnnotationTracking)
        {
            var branches = allOf.EnumerateArray().ToArray();
            var sbTracked = new StringBuilder();
            sbTracked.AppendLine("{");
            sbTracked.AppendLine($"  const _allOfBase = {context.EvaluatedStateExpr}.clone();");
            for (var i = 0; i < branches.Length; i++)
            {
                var hash = context.GetSubschemaHash(branches[i]);
                sbTracked.AppendLine($"  {context.EvaluatedStateExpr}.reset();");
                sbTracked.AppendLine($"  if (!{context.GenerateValidateCall(hash)}) return false;");
                sbTracked.AppendLine($"  const _allOfBranch{i} = {context.EvaluatedStateExpr}.clone();");
            }
            sbTracked.AppendLine($"  {context.EvaluatedStateExpr}.restoreFrom(_allOfBase);");
            for (var i = 0; i < branches.Length; i++)
            {
                sbTracked.AppendLine($"  {context.EvaluatedStateExpr}.mergeFrom(_allOfBranch{i});");
            }
            sbTracked.AppendLine("}");
            return sbTracked.ToString();
        }
        var sb = new StringBuilder();
        foreach (var sub in allOf.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(sub);
            sb.AppendLine($"if (!{context.GenerateValidateCall(hash)}) return false;");
        }
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates TypeScript code for the "anyOf" keyword.
/// </summary>
public sealed class TsAnyOfCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "anyOf";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("anyOf", out var a) &&
        a.ValueKind == JsonValueKind.Array;

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("anyOf", out var anyOf) ||
            anyOf.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }
        if (context.RequiresAnnotationTracking)
        {
            var branches = anyOf.EnumerateArray().ToArray();
            var sbTracked = new StringBuilder();
            sbTracked.AppendLine("{");
            sbTracked.AppendLine($"  const _anyOfBase = {context.EvaluatedStateExpr}.clone();");
            sbTracked.AppendLine("  const _anyOfMatches = [];");
            foreach (var branch in branches)
            {
                var hash = context.GetSubschemaHash(branch);
                sbTracked.AppendLine($"  {context.EvaluatedStateExpr}.reset();");
                sbTracked.AppendLine($"  if ({context.GenerateValidateCall(hash)}) _anyOfMatches.push({context.EvaluatedStateExpr}.clone());");
            }
            sbTracked.AppendLine("  if (_anyOfMatches.length === 0) return false;");
            sbTracked.AppendLine($"  {context.EvaluatedStateExpr}.restoreFrom(_anyOfBase);");
            sbTracked.AppendLine($"  for (const _m of _anyOfMatches) {context.EvaluatedStateExpr}.mergeFrom(_m);");
            sbTracked.AppendLine("}");
            return sbTracked.ToString();
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

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates TypeScript code for the "oneOf" keyword.
/// </summary>
public sealed class TsOneOfCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "oneOf";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("oneOf", out var a) &&
        a.ValueKind == JsonValueKind.Array;

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("oneOf", out var oneOf) ||
            oneOf.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }
        if (context.RequiresAnnotationTracking)
        {
            var branches = oneOf.EnumerateArray().ToArray();
            var sbTracked = new StringBuilder();
            sbTracked.AppendLine("{");
            sbTracked.AppendLine($"  const _oneOfBase = {context.EvaluatedStateExpr}.clone();");
            sbTracked.AppendLine("  let _oneOfMatch = null;");
            sbTracked.AppendLine("  let _matches = 0;");
            foreach (var branch in branches)
            {
                var hash = context.GetSubschemaHash(branch);
                sbTracked.AppendLine($"  {context.EvaluatedStateExpr}.reset();");
                sbTracked.AppendLine($"  if ({context.GenerateValidateCall(hash)}) {{");
                sbTracked.AppendLine("    _matches++;");
                sbTracked.AppendLine("    if (_matches > 1) return false;");
                sbTracked.AppendLine($"    _oneOfMatch = {context.EvaluatedStateExpr}.clone();");
                sbTracked.AppendLine("  }");
            }
            sbTracked.AppendLine("  if (_matches !== 1) return false;");
            sbTracked.AppendLine($"  {context.EvaluatedStateExpr}.restoreFrom(_oneOfBase);");
            sbTracked.AppendLine($"  {context.EvaluatedStateExpr}.mergeFrom(_oneOfMatch);");
            sbTracked.AppendLine("}");
            return sbTracked.ToString();
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

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates TypeScript code for the "not" keyword.
/// </summary>
public sealed class TsNotCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "not";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("not", out _);

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("not", out var notElem))
        {
            return string.Empty;
        }
        var hash = context.GetSubschemaHash(notElem);
        if (context.RequiresAnnotationTracking)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  const _notSnapshot = {context.EvaluatedStateExpr}.clone();");
            sb.AppendLine($"  {context.EvaluatedStateExpr}.reset();");
            sb.AppendLine($"  const _notResult = {context.GenerateValidateCall(hash)};");
            sb.AppendLine($"  {context.EvaluatedStateExpr}.restoreFrom(_notSnapshot);");
            sb.AppendLine("  if (_notResult) return false;");
            sb.AppendLine("}");
            return sb.ToString();
        }
        return $"if ({context.GenerateValidateCall(hash)}) return false;";
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}
