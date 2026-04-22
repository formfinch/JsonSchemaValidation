// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for string constraint keywords: minLength, maxLength.
/// Grapheme counting is delegated to the runtime's graphemeLength helper to match
/// C# StringInfo.LengthInTextElements semantics.
/// </summary>
public sealed class JsStringConstraintsCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "minLength/maxLength";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return false;
        return schema.TryGetProperty("minLength", out _) ||
               schema.TryGetProperty("maxLength", out _);
    }

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            return string.Empty;
        }

        var schema = context.CurrentSchema;
        var hasMin = schema.TryGetProperty("minLength", out var minElem) &&
                     TryGetIntegerValue(minElem, out var min);
        var hasMax = schema.TryGetProperty("maxLength", out var maxElem) &&
                     TryGetIntegerValue(maxElem, out var max);
        if (!hasMin && !hasMax) return string.Empty;

        TryGetIntegerValue(minElem, out min);
        TryGetIntegerValue(maxElem, out max);

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"string\") {{");
        sb.AppendLine($"  const _len = graphemeLength({v});");
        if (hasMin) sb.AppendLine($"  if (_len < {min}) return false;");
        if (hasMax) sb.AppendLine($"  if (_len > {max}) return false;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            yield break;
        }

        // Only yield the import when GenerateCode will actually emit a
        // graphemeLength call — skip for non-integer minLength/maxLength values
        // that GenerateCode ignores, so unused imports don't creep into the
        // emitted module.
        var schema = context.CurrentSchema;
        var hasMin = schema.TryGetProperty("minLength", out var minElem) &&
                     TryGetIntegerValue(minElem, out _);
        var hasMax = schema.TryGetProperty("maxLength", out var maxElem) &&
                     TryGetIntegerValue(maxElem, out _);
        if (hasMin || hasMax)
        {
            yield return "graphemeLength";
        }
    }

    private static bool TryGetIntegerValue(JsonElement element, out long value) =>
        JsSchemaNumeric.TryGetNonNegativeIntegerValue(element, out value);
}
