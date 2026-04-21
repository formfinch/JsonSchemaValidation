// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for the "if"/"then"/"else" keywords (Draft 7+).
/// </summary>
public sealed class JsIfThenElseCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "if/then/else";
    public int Priority => 25;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("if", out _);

    public string GenerateCode(JsCodeGenerationContext context)
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
        if (!hasThen && !hasElse) return string.Empty;

        var ifHash = context.GetSubschemaHash(ifElem);
        var sb = new StringBuilder();
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

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}
