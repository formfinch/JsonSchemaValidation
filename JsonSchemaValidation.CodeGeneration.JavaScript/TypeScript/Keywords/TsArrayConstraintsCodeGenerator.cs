// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for array constraint keywords: minItems, maxItems, uniqueItems.
/// </summary>
public sealed class TsArrayConstraintsCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "minItems/maxItems/uniqueItems";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return false;
        return schema.TryGetProperty("minItems", out _) ||
               schema.TryGetProperty("maxItems", out _) ||
               schema.TryGetProperty("uniqueItems", out _);
    }

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            return string.Empty;
        }

        var schema = context.CurrentSchema;
        var hasMin = schema.TryGetProperty("minItems", out var minElem) &&
                     TryGetIntegerValue(minElem, out var min);
        var hasMax = schema.TryGetProperty("maxItems", out var maxElem) &&
                     TryGetIntegerValue(maxElem, out var max);
        var hasUnique = schema.TryGetProperty("uniqueItems", out var unElem) &&
                        unElem.ValueKind == JsonValueKind.True;

        if (!hasMin && !hasMax && !hasUnique) return string.Empty;

        TryGetIntegerValue(minElem, out min);
        TryGetIntegerValue(maxElem, out max);

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (Array.isArray({v})) {{");
        if (hasMin) sb.AppendLine($"  if ({v}.length < {min}) return false;");
        if (hasMax) sb.AppendLine($"  if ({v}.length > {max}) return false;");
        if (hasUnique)
        {
            sb.AppendLine($"  for (let _i = 0; _i < {v}.length; _i++) {{");
            sb.AppendLine($"    for (let _j = _i + 1; _j < {v}.length; _j++) {{");
            sb.AppendLine($"      if (deepEquals({v}[_i], {v}[_j])) return false;");
            sb.AppendLine($"    }}");
            sb.AppendLine($"  }}");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            yield break;
        }

        if (context.CurrentSchema.ValueKind == JsonValueKind.Object &&
            context.CurrentSchema.TryGetProperty("uniqueItems", out var u) &&
            u.ValueKind == JsonValueKind.True)
        {
            yield return "deepEquals";
        }
    }

    private static bool TryGetIntegerValue(JsonElement element, out long value) =>
        TsSchemaNumeric.TryGetNonNegativeIntegerValue(element, out value);
}
