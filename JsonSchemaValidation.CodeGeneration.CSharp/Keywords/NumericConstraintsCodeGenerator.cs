// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Globalization;
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for numeric constraint keywords: minimum, maximum, exclusiveMinimum, exclusiveMaximum, multipleOf.
/// Also supports Draft 3 "divisibleBy" (alias for multipleOf).
/// </summary>
public sealed class NumericConstraintsCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "minimum/maximum/exclusiveMinimum/exclusiveMaximum/multipleOf/divisibleBy";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return schema.TryGetProperty("minimum", out _) ||
               schema.TryGetProperty("maximum", out _) ||
               schema.TryGetProperty("exclusiveMinimum", out _) ||
               schema.TryGetProperty("exclusiveMaximum", out _) ||
               schema.TryGetProperty("multipleOf", out _) ||
               schema.TryGetProperty("divisibleBy", out _); // Draft 3
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
    {
        var schema = context.CurrentSchema;
        var e = context.ElementVariable;
        var sb = new StringBuilder();

        var hasMin = schema.TryGetProperty("minimum", out var minElement);
        var hasMax = schema.TryGetProperty("maximum", out var maxElement);
        var hasExMin = schema.TryGetProperty("exclusiveMinimum", out var exMinElement);
        var hasExMax = schema.TryGetProperty("exclusiveMaximum", out var exMaxElement);
        var hasMultipleOf = schema.TryGetProperty("multipleOf", out var multipleOfElement);

        // Draft 3 only: divisibleBy is the same as multipleOf
        var hasDivisibleBy = schema.TryGetProperty("divisibleBy", out var divisibleByElement);
        if (!hasMultipleOf && hasDivisibleBy && context.DetectedDraft == SchemaDraft.Draft3)
        {
            hasMultipleOf = true;
            multipleOfElement = divisibleByElement;
        }

        if (!hasMin && !hasMax && !hasExMin && !hasExMax && !hasMultipleOf)
        {
            return string.Empty;
        }

        // Check if we need min/max constraints
        var needsMinMax = hasMin || hasMax || hasExMin || hasExMax;

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Number)");
        sb.AppendLine("{");

        if (needsMinMax)
        {
            // Use GetDouble() to match dynamic validator behavior and handle large values
            sb.AppendLine($"    var _num_ = {e}.GetDouble();");

            if (hasMin && minElement.TryGetDouble(out var min))
            {
                sb.AppendLine($"    if (_num_ < {FormatDouble(min)}) return false;");
            }

            if (hasMax && maxElement.TryGetDouble(out var max))
            {
                sb.AppendLine($"    if (_num_ > {FormatDouble(max)}) return false;");
            }

            // Handle exclusiveMinimum - can be number (2020-12) or boolean (draft 4)
            if (hasExMin)
            {
                if (exMinElement.ValueKind == JsonValueKind.Number && exMinElement.TryGetDouble(out var exMin))
                {
                    sb.AppendLine($"    if (_num_ <= {FormatDouble(exMin)}) return false;");
                }
                else if (exMinElement.ValueKind == JsonValueKind.True && hasMin && minElement.TryGetDouble(out var minVal))
                {
                    sb.AppendLine($"    if (_num_ <= {FormatDouble(minVal)}) return false;");
                }
            }

            // Handle exclusiveMaximum - can be number (2020-12) or boolean (draft 4)
            if (hasExMax)
            {
                if (exMaxElement.ValueKind == JsonValueKind.Number && exMaxElement.TryGetDouble(out var exMax))
                {
                    sb.AppendLine($"    if (_num_ >= {FormatDouble(exMax)}) return false;");
                }
                else if (exMaxElement.ValueKind == JsonValueKind.True && hasMax && maxElement.TryGetDouble(out var maxVal))
                {
                    sb.AppendLine($"    if (_num_ >= {FormatDouble(maxVal)}) return false;");
                }
            }
        }

        if (hasMultipleOf && multipleOfElement.TryGetDouble(out var multipleOf) && multipleOf != 0)
        {
            // Use double for multipleOf to handle overflow cases (matching dynamic validator logic)
            var divisor = multipleOf.ToString(CultureInfo.InvariantCulture);
            // Reuse _num_ if already declared, otherwise get it
            if (!needsMinMax)
            {
                sb.AppendLine($"    var _num_ = {e}.GetDouble();");
            }
            sb.AppendLine($"    var _divisor_ = {divisor};");
            sb.AppendLine($"    if (Math.Abs(_num_ % _divisor_) >= double.Epsilon)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var _quotient_ = _num_ / _divisor_;");
            sb.AppendLine($"        // Handle overflow: if quotient is infinity and value is integer and 1 is multiple of divisor");
            sb.AppendLine($"        if (!(double.IsInfinity(_quotient_) && Math.Abs(_num_ % 1) < double.Epsilon && Math.Abs(1.0 % _divisor_) < double.Epsilon))");
            sb.AppendLine("        {");
            sb.AppendLine($"            _quotient_ = Math.Round((_quotient_ + 0.000001) * 100) / 100.0;");
            sb.AppendLine($"            if (!(Math.Abs(_quotient_ - Math.Round(_quotient_)) < double.Epsilon)) return false;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CSharpCodeGenerationContext context)
    {
        return [];
    }

    private static string FormatDouble(double value)
    {
        // Use G17 to preserve full precision for round-trip
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }
}
