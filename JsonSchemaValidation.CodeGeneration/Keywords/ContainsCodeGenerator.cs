using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "contains" keyword with minContains/maxContains support.
/// </summary>
public sealed class ContainsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "contains";
    public int Priority => 35;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("contains", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("contains", out var containsElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var containsHash = context.GetSubschemaHash(containsElement);

        // Get minContains/maxContains (defaults: minContains=1, maxContains=unlimited)
        var minContains = 1;
        var maxContains = int.MaxValue;

        if (context.CurrentSchema.TryGetProperty("minContains", out var minElement) &&
            minElement.TryGetInt32(out var min))
        {
            minContains = min;
        }

        if (context.CurrentSchema.TryGetProperty("maxContains", out var maxElement) &&
            maxElement.TryGetInt32(out var max))
        {
            maxContains = max;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("{");
        sb.AppendLine("    var _containsCount_ = 0;");
        sb.AppendLine($"    foreach (var _containsItem_ in {e}.EnumerateArray())");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (Validate_{containsHash}(_containsItem_))");
        sb.AppendLine("        {");
        sb.AppendLine("            _containsCount_++;");

        if (maxContains != int.MaxValue)
        {
            sb.AppendLine($"            if (_containsCount_ > {maxContains}) return false;");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine($"    if (_containsCount_ < {minContains}) return false;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
