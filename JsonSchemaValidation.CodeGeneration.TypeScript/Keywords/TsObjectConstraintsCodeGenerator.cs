// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for object constraint keywords: minProperties, maxProperties.
/// </summary>
public sealed class TsObjectConstraintsCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "minProperties/maxProperties";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return false;
        return schema.TryGetProperty("minProperties", out _) ||
               schema.TryGetProperty("maxProperties", out _);
    }

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            return string.Empty;
        }

        var schema = context.CurrentSchema;
        var hasMin = schema.TryGetProperty("minProperties", out var minElem) &&
                     TryGetIntegerValue(minElem, out var min);
        var hasMax = schema.TryGetProperty("maxProperties", out var maxElem) &&
                     TryGetIntegerValue(maxElem, out var max);
        if (!hasMin && !hasMax) return string.Empty;

        TryGetIntegerValue(minElem, out min);
        TryGetIntegerValue(maxElem, out max);

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        sb.AppendLine($"  const _keys = Object.keys({v});");
        if (hasMin) sb.AppendLine($"  if (_keys.length < {min}) return false;");
        if (hasMax) sb.AppendLine($"  if (_keys.length > {max}) return false;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];

    private static bool TryGetIntegerValue(JsonElement element, out long value) =>
        TsSchemaNumeric.TryGetNonNegativeIntegerValue(element, out value);
}
