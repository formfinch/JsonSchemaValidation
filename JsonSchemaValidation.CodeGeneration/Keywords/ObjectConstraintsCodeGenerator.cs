using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for object constraint keywords: minProperties, maxProperties.
/// </summary>
public sealed class ObjectConstraintsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "minProperties/maxProperties";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return schema.TryGetProperty("minProperties", out _) ||
               schema.TryGetProperty("maxProperties", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        var schema = context.CurrentSchema;
        var e = context.ElementVariable;
        var sb = new StringBuilder();

        var hasMinProperties = schema.TryGetProperty("minProperties", out var minPropertiesElement);
        var hasMaxProperties = schema.TryGetProperty("maxProperties", out var maxPropertiesElement);

        if (!hasMinProperties && !hasMaxProperties)
        {
            return string.Empty;
        }

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
        sb.AppendLine("{");
        sb.AppendLine($"    var _propCount_ = 0;");
        sb.AppendLine($"    foreach (var _ in {e}.EnumerateObject()) _propCount_++;");

        if (hasMinProperties && TryGetIntegerValue(minPropertiesElement, out var minProperties))
        {
            sb.AppendLine($"    if (_propCount_ < {minProperties}) return false;");
        }

        if (hasMaxProperties && TryGetIntegerValue(maxPropertiesElement, out var maxProperties))
        {
            sb.AppendLine($"    if (_propCount_ > {maxProperties}) return false;");
        }

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
