using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "items" keyword (2020-12 style - single schema).
/// </summary>
public sealed class ItemsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "items";
    public int Priority => 40;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!schema.TryGetProperty("items", out var items))
        {
            return false;
        }

        // Handle 2020-12 style (single schema) or check if it's not array style
        return items.ValueKind == JsonValueKind.Object ||
               items.ValueKind == JsonValueKind.True ||
               items.ValueKind == JsonValueKind.False;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("items", out var itemsElement))
        {
            return string.Empty;
        }

        // Skip if this is array-style items (draft 4/6/7)
        if (itemsElement.ValueKind == JsonValueKind.Array)
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var hash = context.GetSubschemaHash(itemsElement);

        // Check if prefixItems exists (2020-12) - if so, items only validates after prefixItems
        var hasPrefixItems = context.CurrentSchema.TryGetProperty("prefixItems", out var prefixItemsElement);
        var prefixCount = 0;
        if (hasPrefixItems && prefixItemsElement.ValueKind == JsonValueKind.Array)
        {
            prefixCount = prefixItemsElement.GetArrayLength();
        }

        var sb = new StringBuilder();
        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("{");

        if (prefixCount > 0)
        {
            sb.AppendLine($"    var _itemIdx_ = 0;");
            sb.AppendLine($"    foreach (var _arrItem_ in {e}.EnumerateArray())");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (_itemIdx_ >= {prefixCount})");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (!Validate_{hash}(_arrItem_)) return false;");
            sb.AppendLine("        }");
            sb.AppendLine("        _itemIdx_++;");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine($"    foreach (var _arrItem_ in {e}.EnumerateArray())");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!Validate_{hash}(_arrItem_)) return false;");
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

/// <summary>
/// Generates code for the "prefixItems" keyword (2020-12).
/// </summary>
public sealed class PrefixItemsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "prefixItems";
    public int Priority => 45; // Before items

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("prefixItems", out var arr) &&
               arr.ValueKind == JsonValueKind.Array;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("prefixItems", out var prefixItemsElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("{");
        sb.AppendLine("    var _prefixIdx_ = 0;");
        sb.AppendLine($"    foreach (var _prefixItem_ in {e}.EnumerateArray())");
        sb.AppendLine("    {");

        var idx = 0;
        foreach (var subschema in prefixItemsElement.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(subschema);
            sb.AppendLine($"        if (_prefixIdx_ == {idx})");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (!Validate_{hash}(_prefixItem_)) return false;");
            sb.AppendLine("        }");
            idx++;
        }

        sb.AppendLine($"        if (_prefixIdx_ >= {idx}) break;");
        sb.AppendLine("        _prefixIdx_++;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
