using System.Globalization;
using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for numeric constraint keywords: minimum, maximum, exclusiveMinimum, exclusiveMaximum, multipleOf.
/// </summary>
public sealed class NumericConstraintsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "minimum/maximum/exclusiveMinimum/exclusiveMaximum/multipleOf";
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
               schema.TryGetProperty("multipleOf", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        var schema = context.CurrentSchema;
        var e = context.ElementVariable;
        var sb = new StringBuilder();

        var hasMin = schema.TryGetProperty("minimum", out var minElement);
        var hasMax = schema.TryGetProperty("maximum", out var maxElement);
        var hasExMin = schema.TryGetProperty("exclusiveMinimum", out var exMinElement);
        var hasExMax = schema.TryGetProperty("exclusiveMaximum", out var exMaxElement);
        var hasMultipleOf = schema.TryGetProperty("multipleOf", out var multipleOfElement);

        if (!hasMin && !hasMax && !hasExMin && !hasExMax && !hasMultipleOf)
        {
            return string.Empty;
        }

        // Check if we need decimal for min/max constraints
        var needsDecimal = hasMin || hasMax || hasExMin || hasExMax;

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Number)");
        sb.AppendLine("{");

        if (needsDecimal)
        {
            // Use TryGetDecimal to handle values that overflow decimal range
            sb.AppendLine($"    if ({e}.TryGetDecimal(out var _num_))");
            sb.AppendLine("    {");

            if (hasMin && minElement.TryGetDecimal(out var min))
            {
                sb.AppendLine($"        if (_num_ < {FormatDecimal(min)}m) return false;");
            }

            if (hasMax && maxElement.TryGetDecimal(out var max))
            {
                sb.AppendLine($"        if (_num_ > {FormatDecimal(max)}m) return false;");
            }

            // Handle exclusiveMinimum - can be number (2020-12) or boolean (draft 4)
            if (hasExMin)
            {
                if (exMinElement.ValueKind == JsonValueKind.Number && exMinElement.TryGetDecimal(out var exMin))
                {
                    sb.AppendLine($"        if (_num_ <= {FormatDecimal(exMin)}m) return false;");
                }
                else if (exMinElement.ValueKind == JsonValueKind.True && hasMin && minElement.TryGetDecimal(out var minVal))
                {
                    sb.AppendLine($"        if (_num_ <= {FormatDecimal(minVal)}m) return false;");
                }
            }

            // Handle exclusiveMaximum - can be number (2020-12) or boolean (draft 4)
            if (hasExMax)
            {
                if (exMaxElement.ValueKind == JsonValueKind.Number && exMaxElement.TryGetDecimal(out var exMax))
                {
                    sb.AppendLine($"        if (_num_ >= {FormatDecimal(exMax)}m) return false;");
                }
                else if (exMaxElement.ValueKind == JsonValueKind.True && hasMax && maxElement.TryGetDecimal(out var maxVal))
                {
                    sb.AppendLine($"        if (_num_ >= {FormatDecimal(maxVal)}m) return false;");
                }
            }

            sb.AppendLine("    }");
        }

        if (hasMultipleOf && multipleOfElement.TryGetDouble(out var multipleOf) && multipleOf != 0)
        {
            // Use double for multipleOf to handle overflow cases (matching dynamic validator logic)
            var divisor = multipleOf.ToString(CultureInfo.InvariantCulture);
            sb.AppendLine($"    var _numD_ = {e}.GetDouble();");
            sb.AppendLine($"    var _divisor_ = {divisor};");
            sb.AppendLine($"    if (Math.Abs(_numD_ % _divisor_) >= double.Epsilon)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var _quotient_ = _numD_ / _divisor_;");
            sb.AppendLine($"        // Handle overflow: if quotient is infinity and value is integer and 1 is multiple of divisor");
            sb.AppendLine($"        if (!(double.IsInfinity(_quotient_) && Math.Abs(_numD_ % 1) < double.Epsilon && Math.Abs(1.0 % _divisor_) < double.Epsilon))");
            sb.AppendLine("        {");
            sb.AppendLine($"            _quotient_ = Math.Round((_quotient_ + 0.000001) * 100) / 100.0;");
            sb.AppendLine($"            if (!(Math.Abs(_quotient_ - Math.Round(_quotient_)) < double.Epsilon)) return false;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
