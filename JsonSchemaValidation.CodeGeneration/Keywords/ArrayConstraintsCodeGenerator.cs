using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for array constraint keywords: minItems, maxItems, uniqueItems.
/// </summary>
public sealed class ArrayConstraintsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "minItems/maxItems/uniqueItems";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return schema.TryGetProperty("minItems", out _) ||
               schema.TryGetProperty("maxItems", out _) ||
               schema.TryGetProperty("uniqueItems", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        var schema = context.CurrentSchema;
        var e = context.ElementVariable;
        var sb = new StringBuilder();

        var hasMinItems = schema.TryGetProperty("minItems", out var minItemsElement);
        var hasMaxItems = schema.TryGetProperty("maxItems", out var maxItemsElement);
        var hasUniqueItems = schema.TryGetProperty("uniqueItems", out var uniqueItemsElement);

        if (!hasMinItems && !hasMaxItems && !hasUniqueItems)
        {
            return string.Empty;
        }

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("{");
        sb.AppendLine($"    var _arrLen_ = {e}.GetArrayLength();");

        if (hasMinItems && minItemsElement.TryGetInt32(out var minItems))
        {
            sb.AppendLine($"    if (_arrLen_ < {minItems}) return false;");
        }

        if (hasMaxItems && maxItemsElement.TryGetInt32(out var maxItems))
        {
            sb.AppendLine($"    if (_arrLen_ > {maxItems}) return false;");
        }

        if (hasUniqueItems && uniqueItemsElement.ValueKind == JsonValueKind.True)
        {
            sb.AppendLine("    // Check uniqueItems");
            sb.AppendLine($"    var _items_ = new List<JsonElement>(_arrLen_);");
            sb.AppendLine($"    foreach (var _item_ in {e}.EnumerateArray())");
            sb.AppendLine("    {");
            sb.AppendLine("        foreach (var _existing_ in _items_)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (JsonElementDeepEquals(_item_, _existing_)) return false;");
            sb.AppendLine("        }");
            sb.AppendLine("        _items_.Add(_item_);");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
