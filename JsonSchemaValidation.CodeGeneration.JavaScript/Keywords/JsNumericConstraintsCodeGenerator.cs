// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for numeric constraint keywords:
/// minimum, maximum, exclusiveMinimum, exclusiveMaximum, multipleOf.
/// Handles Draft 4's boolean exclusiveMinimum/exclusiveMaximum form by pairing
/// with the sibling minimum/maximum value.
/// multipleOf mirrors the C# compiled path's IEEE-754 quotient-snapping logic.
/// </summary>
public sealed class JsNumericConstraintsCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "minimum/maximum/exclusiveMinimum/exclusiveMaximum/multipleOf";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return false;
        return schema.TryGetProperty("minimum", out _) ||
               schema.TryGetProperty("maximum", out _) ||
               schema.TryGetProperty("exclusiveMinimum", out _) ||
               schema.TryGetProperty("exclusiveMaximum", out _) ||
               schema.TryGetProperty("multipleOf", out _);
    }

    public string GenerateCode(JsCodeGenerationContext context)
    {
        var schema = context.CurrentSchema;
        var hasMin = schema.TryGetProperty("minimum", out var minElem);
        var hasMax = schema.TryGetProperty("maximum", out var maxElem);
        var hasExMin = schema.TryGetProperty("exclusiveMinimum", out var exMinElem);
        var hasExMax = schema.TryGetProperty("exclusiveMaximum", out var exMaxElem);
        var hasMul = schema.TryGetProperty("multipleOf", out var mulElem);

        if (!hasMin && !hasMax && !hasExMin && !hasExMax && !hasMul) return string.Empty;

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"number\") {{");

        if (hasMin && minElem.TryGetDouble(out var min))
        {
            sb.AppendLine($"  if ({v} < {Fmt(min)}) return false;");
        }
        if (hasMax && maxElem.TryGetDouble(out var max))
        {
            sb.AppendLine($"  if ({v} > {Fmt(max)}) return false;");
        }

        if (hasExMin)
        {
            if (exMinElem.ValueKind == JsonValueKind.Number && exMinElem.TryGetDouble(out var exMin))
            {
                sb.AppendLine($"  if ({v} <= {Fmt(exMin)}) return false;");
            }
            else if (exMinElem.ValueKind == JsonValueKind.True && hasMin && minElem.TryGetDouble(out var minVal))
            {
                // Draft 4: exclusiveMinimum: true alongside minimum.
                sb.AppendLine($"  if ({v} <= {Fmt(minVal)}) return false;");
            }
        }

        if (hasExMax)
        {
            if (exMaxElem.ValueKind == JsonValueKind.Number && exMaxElem.TryGetDouble(out var exMax))
            {
                sb.AppendLine($"  if ({v} >= {Fmt(exMax)}) return false;");
            }
            else if (exMaxElem.ValueKind == JsonValueKind.True && hasMax && maxElem.TryGetDouble(out var maxVal))
            {
                sb.AppendLine($"  if ({v} >= {Fmt(maxVal)}) return false;");
            }
        }

        if (hasMul && mulElem.TryGetDouble(out var mul) && mul != 0)
        {
            var divisor = Fmt(mul);
            sb.AppendLine($"  {{");
            sb.AppendLine($"    if (Math.abs({v} % {divisor}) >= Number.EPSILON) {{");
            sb.AppendLine($"      let _q = {v} / {divisor};");
            sb.AppendLine($"      if (!(!isFinite(_q) && Math.abs({v} % 1) < Number.EPSILON && Math.abs(1 % {divisor}) < Number.EPSILON)) {{");
            sb.AppendLine($"        _q = Math.round((_q + 0.000001) * 100) / 100;");
            sb.AppendLine($"        if (!(Math.abs(_q - Math.round(_q)) < Number.EPSILON)) return false;");
            sb.AppendLine($"      }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"  }}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];

    private static string Fmt(double d) => d.ToString("G17", CultureInfo.InvariantCulture);
}
