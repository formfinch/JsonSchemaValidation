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

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Number)");
        sb.AppendLine("{");
        sb.AppendLine($"    var _num_ = {e}.GetDecimal();");

        if (hasMin && minElement.TryGetDecimal(out var min))
        {
            sb.AppendLine($"    if (_num_ < {FormatDecimal(min)}m) return false;");
        }

        if (hasMax && maxElement.TryGetDecimal(out var max))
        {
            sb.AppendLine($"    if (_num_ > {FormatDecimal(max)}m) return false;");
        }

        // Handle exclusiveMinimum - can be number (2020-12) or boolean (draft 4)
        if (hasExMin)
        {
            if (exMinElement.ValueKind == JsonValueKind.Number && exMinElement.TryGetDecimal(out var exMin))
            {
                sb.AppendLine($"    if (_num_ <= {FormatDecimal(exMin)}m) return false;");
            }
            else if (exMinElement.ValueKind == JsonValueKind.True && hasMin && minElement.TryGetDecimal(out var minVal))
            {
                // Draft 4 style: exclusiveMinimum: true with minimum
                sb.AppendLine($"    if (_num_ <= {FormatDecimal(minVal)}m) return false;");
            }
        }

        // Handle exclusiveMaximum - can be number (2020-12) or boolean (draft 4)
        if (hasExMax)
        {
            if (exMaxElement.ValueKind == JsonValueKind.Number && exMaxElement.TryGetDecimal(out var exMax))
            {
                sb.AppendLine($"    if (_num_ >= {FormatDecimal(exMax)}m) return false;");
            }
            else if (exMaxElement.ValueKind == JsonValueKind.True && hasMax && maxElement.TryGetDecimal(out var maxVal))
            {
                // Draft 4 style: exclusiveMaximum: true with maximum
                sb.AppendLine($"    if (_num_ >= {FormatDecimal(maxVal)}m) return false;");
            }
        }

        if (hasMultipleOf && multipleOfElement.TryGetDecimal(out var multipleOf) && multipleOf != 0)
        {
            sb.AppendLine($"    if (_num_ % {FormatDecimal(multipleOf)}m != 0m) return false;");
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
