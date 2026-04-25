// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for the "if"/"then"/"else" keywords (Draft 7+).
/// </summary>
public sealed class TsIfThenElseCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "if/then/else";
    public int Priority => 25;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("if", out _);

    public string GenerateCode(TsCodeGenerationContext context)
    {
        // if/then/else were introduced in Draft 7; earlier drafts must ignore them.
        if (context.DetectedDraft < SchemaDraft.Draft7) return string.Empty;
        var schema = context.CurrentSchema;
        if (!schema.TryGetProperty("if", out var ifElem))
        {
            return string.Empty;
        }
        var hasThen = schema.TryGetProperty("then", out var thenElem);
        var hasElse = schema.TryGetProperty("else", out var elseElem);
        if (!hasThen && !hasElse && !context.RequiresAnnotationTracking) return string.Empty;

        var ifHash = context.GetSubschemaHash(ifElem);
        var sb = new StringBuilder();
        if (context.RequiresAnnotationTracking)
        {
            sb.AppendLine("{");
            sb.AppendLine($"  const _ifteBase = {context.EvaluatedStateExpr}.clone();");
            sb.AppendLine($"  {context.EvaluatedStateExpr}.reset();");
            sb.AppendLine($"  const _ifResult = {context.GenerateValidateCall(ifHash)};");
            sb.AppendLine($"  const _ifAnn = {context.EvaluatedStateExpr}.clone();");

            sb.AppendLine("  if (_ifResult) {");
            if (hasThen)
            {
                var thenHash = context.GetSubschemaHash(thenElem);
                sb.AppendLine($"    {context.EvaluatedStateExpr}.reset();");
                sb.AppendLine($"    if (!{context.GenerateValidateCall(thenHash)}) return false;");
                sb.AppendLine($"    const _thenAnn = {context.EvaluatedStateExpr}.clone();");
                sb.AppendLine($"    {context.EvaluatedStateExpr}.restoreFrom(_ifteBase);");
                sb.AppendLine($"    {context.EvaluatedStateExpr}.mergeFrom(_ifAnn);");
                sb.AppendLine($"    {context.EvaluatedStateExpr}.mergeFrom(_thenAnn);");
            }
            else
            {
                sb.AppendLine($"    {context.EvaluatedStateExpr}.restoreFrom(_ifteBase);");
                sb.AppendLine($"    {context.EvaluatedStateExpr}.mergeFrom(_ifAnn);");
            }
            sb.AppendLine("  } else {");
            if (hasElse)
            {
                var elseHash = context.GetSubschemaHash(elseElem);
                sb.AppendLine($"    {context.EvaluatedStateExpr}.reset();");
                sb.AppendLine($"    if (!{context.GenerateValidateCall(elseHash)}) return false;");
                sb.AppendLine($"    const _elseAnn = {context.EvaluatedStateExpr}.clone();");
                sb.AppendLine($"    {context.EvaluatedStateExpr}.restoreFrom(_ifteBase);");
                sb.AppendLine($"    {context.EvaluatedStateExpr}.mergeFrom(_elseAnn);");
            }
            else
            {
                sb.AppendLine($"    {context.EvaluatedStateExpr}.restoreFrom(_ifteBase);");
            }
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }
        sb.AppendLine($"if ({context.GenerateValidateCall(ifHash)}) {{");
        if (hasThen)
        {
            var thenHash = context.GetSubschemaHash(thenElem);
            sb.AppendLine($"  if (!{context.GenerateValidateCall(thenHash)}) return false;");
        }
        sb.AppendLine("} else {");
        if (hasElse)
        {
            var elseHash = context.GetSubschemaHash(elseElem);
            sb.AppendLine($"  if (!{context.GenerateValidateCall(elseHash)}) return false;");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}
