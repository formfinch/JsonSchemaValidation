using System.Globalization;
using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for string constraint keywords: minLength, maxLength.
/// </summary>
public sealed class StringConstraintsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "minLength/maxLength";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return schema.TryGetProperty("minLength", out _) ||
               schema.TryGetProperty("maxLength", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        var schema = context.CurrentSchema;
        var e = context.ElementVariable;
        var sb = new StringBuilder();

        var hasMinLength = schema.TryGetProperty("minLength", out var minLengthElement);
        var hasMaxLength = schema.TryGetProperty("maxLength", out var maxLengthElement);

        if (!hasMinLength && !hasMaxLength)
        {
            return string.Empty;
        }

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.String)");
        sb.AppendLine("{");
        sb.AppendLine($"    var _str_ = {e}.GetString();");
        sb.AppendLine("    if (_str_ != null)");
        sb.AppendLine("    {");
        sb.AppendLine("        var _len_ = new StringInfo(_str_).LengthInTextElements;");

        if (hasMinLength && TryGetIntegerValue(minLengthElement, out var minLength))
        {
            sb.AppendLine($"        if (_len_ < {minLength}) return false;");
        }

        if (hasMaxLength && TryGetIntegerValue(maxLengthElement, out var maxLength))
        {
            sb.AppendLine($"        if (_len_ > {maxLength}) return false;");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }

    private static bool TryGetIntegerValue(JsonElement element, out long value)
    {
        value = 0;
        if (element.ValueKind != JsonValueKind.Number) return false;
        if (!element.TryGetDouble(out var doubleValue)) return false;
        if (doubleValue < 0 || Math.Abs(doubleValue - Math.Floor(doubleValue)) > double.Epsilon) return false;
        value = (long)doubleValue;
        return true;
    }
}
