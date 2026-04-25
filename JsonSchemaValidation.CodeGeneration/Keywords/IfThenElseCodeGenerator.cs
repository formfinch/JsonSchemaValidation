// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "if/then/else" keywords.
/// </summary>
public sealed class IfThenElseCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "if/then/else";
    public int Priority => 25;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("if", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        // if/then/else was introduced in Draft 7
        if (context.DetectedDraft < SchemaDraft.Draft7)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("if", out var ifElement))
        {
            return string.Empty;
        }

        var hasThen = context.CurrentSchema.TryGetProperty("then", out var thenElement);
        var hasElse = context.CurrentSchema.TryGetProperty("else", out var elseElement);

        var e = context.ElementVariable;
        var ifHash = context.GetSubschemaHash(ifElement);
        var sb = new StringBuilder();

        var eval = context.EvaluatedStateVariable;

        // If annotation tracking is enabled, each subschema starts with fresh annotations
        if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
        {
            // Even without then/else, "if" must be evaluated for annotation collection
            if (!hasThen && !hasElse)
            {
                // Evaluate "if" for its annotations (ignore the boolean result)
                sb.AppendLine("// if (without then/else): evaluate for annotations");
                sb.AppendLine("{");
                sb.AppendLine($"    var _ifteBase_ = {eval}.Clone();");
                sb.AppendLine($"    {eval}.Reset();");
                sb.AppendLine($"    {context.GenerateValidateCall(ifHash)};");
                sb.AppendLine($"    var _ifAnn_ = {eval}.Clone();");
                sb.AppendLine($"    {eval}.RestoreFrom(_ifteBase_);");
                sb.AppendLine($"    {eval}.MergeFrom(_ifAnn_);");
                sb.AppendLine("}");
                return sb.ToString();
            }

            sb.AppendLine("// if/then/else (with annotation isolation)");
            sb.AppendLine("{");
            sb.AppendLine($"    var _ifteBase_ = {eval}.Clone();");
            sb.AppendLine($"    {eval}.Reset();");
            sb.AppendLine($"    var _ifResult_ = {context.GenerateValidateCall(ifHash)};");
            sb.AppendLine($"    var _ifAnn_ = {eval}.Clone();");

            if (hasThen)
            {
                var thenHash = context.GetSubschemaHash(thenElement);
                sb.AppendLine("    if (_ifResult_)");
                sb.AppendLine("    {");
                sb.AppendLine($"        {eval}.Reset();");
                sb.AppendLine($"        if (!{context.GenerateValidateCall(thenHash)}) return false;");
                sb.AppendLine($"        var _thenAnn_ = {eval}.Clone();");
                sb.AppendLine($"        {eval}.RestoreFrom(_ifteBase_);");
                sb.AppendLine($"        {eval}.MergeFrom(_ifAnn_);");
                sb.AppendLine($"        {eval}.MergeFrom(_thenAnn_);");
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine("    if (_ifResult_)");
                sb.AppendLine("    {");
                sb.AppendLine($"        {eval}.RestoreFrom(_ifteBase_);");
                sb.AppendLine($"        {eval}.MergeFrom(_ifAnn_);");
                sb.AppendLine("    }");
            }

            if (hasElse)
            {
                var elseHash = context.GetSubschemaHash(elseElement);
                sb.AppendLine("    else");
                sb.AppendLine("    {");
                sb.AppendLine($"        {eval}.Reset();");
                sb.AppendLine($"        if (!{context.GenerateValidateCall(elseHash)}) return false;");
                sb.AppendLine($"        var _elseAnn_ = {eval}.Clone();");
                sb.AppendLine($"        {eval}.RestoreFrom(_ifteBase_);");
                sb.AppendLine($"        {eval}.MergeFrom(_ifAnn_);");
                sb.AppendLine($"        {eval}.MergeFrom(_elseAnn_);");
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine("    else");
                sb.AppendLine("    {");
                sb.AppendLine($"        {eval}.RestoreFrom(_ifteBase_);");
                sb.AppendLine($"        {eval}.MergeFrom(_ifAnn_);");
                sb.AppendLine("    }");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        // No annotation tracking - simple version
        if (!hasThen && !hasElse)
        {
            return string.Empty; // if alone has no validation effect without annotations
        }

        sb.AppendLine("// if/then/else");
        sb.AppendLine($"if ({context.GenerateValidateCall(ifHash)})");
        sb.AppendLine("{");

        if (hasThen)
        {
            var thenHash = context.GetSubschemaHash(thenElement);
            sb.AppendLine($"    if (!{context.GenerateValidateCall(thenHash)}) return false;");
        }

        sb.AppendLine("}");

        if (hasElse)
        {
            sb.AppendLine("else");
            sb.AppendLine("{");
            var elseHash = context.GetSubschemaHash(elseElement);
            sb.AppendLine($"    if (!{context.GenerateValidateCall(elseHash)}) return false;");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
