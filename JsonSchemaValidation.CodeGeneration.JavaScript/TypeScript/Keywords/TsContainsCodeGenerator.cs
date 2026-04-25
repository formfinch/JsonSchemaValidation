// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for the "contains" keyword with minContains/maxContains.
/// contains was introduced in Draft 6; minContains/maxContains in Draft 2019-09.
/// Draft 4 (MVP support) does not have this keyword — emitter no-ops for Draft 4.
/// </summary>
public sealed class TsContainsCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "contains";
    public int Priority => 35;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("contains", out _);

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (context.DetectedDraft < SchemaDraft.Draft6) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("contains", out var containsElem))
        {
            return string.Empty;
        }

        var hash = context.GetSubschemaHash(containsElem);
        long min = 1;
        long max = long.MaxValue;
        if (context.DetectedDraft >= SchemaDraft.Draft201909 && context.ValidationVocabularyEnabled)
        {
            if (context.CurrentSchema.TryGetProperty("minContains", out var minElem) &&
                TryGetIntegerValue(minElem, out var mn)) min = mn;
            if (context.CurrentSchema.TryGetProperty("maxContains", out var maxElem) &&
                TryGetIntegerValue(maxElem, out var mx)) max = mx;
        }

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (Array.isArray({v})) {{");
        sb.AppendLine("  let _count = 0;");
        sb.AppendLine($"  for (let _i = 0; _i < {v}.length; _i++) {{");
        sb.AppendLine($"    if ({context.GenerateValidateCallForItem(hash, $"{v}[_i]", "_i")}) {{");
        sb.AppendLine("      _count++;");
        if (context.RequiresItemAnnotations)
        {
            sb.AppendLine($"      {context.EvaluatedStateExpr}.markItemEvaluated({context.LocationExpr}, _i);");
        }
        if (max != long.MaxValue)
        {
            sb.AppendLine($"      if (_count > {max}) return false;");
        }
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine($"  if (_count < {min}) return false;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];

    private static bool TryGetIntegerValue(JsonElement element, out long value) =>
        TsSchemaNumeric.TryGetNonNegativeIntegerValue(element, out value);
}
