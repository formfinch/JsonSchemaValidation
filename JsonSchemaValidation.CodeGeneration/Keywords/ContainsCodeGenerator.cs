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
        var eval = context.EvaluatedStateVariable;
        var trackAnnotations = context.RequiresItemAnnotations;
        var containsHash = context.GetSubschemaHash(containsElement);

        // Get minContains/maxContains (defaults: minContains=1, maxContains=unlimited)
        var minContains = 1L;
        var maxContains = (long)int.MaxValue;

        if (context.CurrentSchema.TryGetProperty("minContains", out var minElement) &&
            TryGetIntegerValue(minElement, out var min))
        {
            minContains = min;
        }

        if (context.CurrentSchema.TryGetProperty("maxContains", out var maxElement) &&
            TryGetIntegerValue(maxElement, out var max))
        {
            maxContains = max;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("{");
        sb.AppendLine("    var _containsCount_ = 0;");
        if (trackAnnotations)
        {
            sb.AppendLine("    var _containsIdx_ = 0;");
        }
        sb.AppendLine($"    foreach (var _containsItem_ in {e}.EnumerateArray())");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (Validate_{containsHash}(_containsItem_))");
        sb.AppendLine("        {");
        sb.AppendLine("            _containsCount_++;");
        if (trackAnnotations)
        {
            sb.AppendLine($"            {eval}.EvaluatedItemIndices.Add(_containsIdx_);");
        }

        if (maxContains != (long)int.MaxValue)
        {
            sb.AppendLine($"            if (_containsCount_ > {maxContains}) return false;");
        }

        sb.AppendLine("        }");
        if (trackAnnotations)
        {
            sb.AppendLine("        _containsIdx_++;");
        }
        sb.AppendLine("    }");
        sb.AppendLine($"    if (_containsCount_ < {minContains}) return false;");
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
