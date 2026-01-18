using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "type" keyword.
/// </summary>
public sealed class TypeCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "type";
    public int Priority => 100; // Run first - type check is fast fail

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("type", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("type", out var typeElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return GenerateSingleTypeCheck(typeElement.GetString()!, e);
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            return GenerateMultiTypeCheck(typeElement, e);
        }

        return string.Empty;
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }

    private static string GenerateSingleTypeCheck(string type, string e)
    {
        return type switch
        {
            "string" => $"if ({e}.ValueKind != JsonValueKind.String) return false;",
            "number" => $"if ({e}.ValueKind != JsonValueKind.Number) return false;",
            "integer" => GenerateIntegerCheck(e),
            "boolean" => $"if ({e}.ValueKind != JsonValueKind.True && {e}.ValueKind != JsonValueKind.False) return false;",
            "null" => $"if ({e}.ValueKind != JsonValueKind.Null) return false;",
            "array" => $"if ({e}.ValueKind != JsonValueKind.Array) return false;",
            "object" => $"if ({e}.ValueKind != JsonValueKind.Object) return false;",
            _ => string.Empty
        };
    }

    private static string GenerateIntegerCheck(string e)
    {
        // Match dynamic validator logic: try decimal first, then BigInteger, then double for overflow cases
        return $$"""
if ({{e}}.ValueKind != JsonValueKind.Number) return false;
{
    var _isInt_ = false;
    if ({{e}}.TryGetDecimal(out var _intVal_) && _intVal_ == decimal.Truncate(_intVal_)) _isInt_ = true;
    else if (System.Numerics.BigInteger.TryParse({{e}}.ToString(), System.Globalization.CultureInfo.InvariantCulture, out _)) _isInt_ = true;
    else if ({{e}}.TryGetDouble(out var _dblVal_) && !double.IsInfinity(_dblVal_) && !double.IsNaN(_dblVal_) && Math.Abs(_dblVal_ - Math.Floor(_dblVal_)) < double.Epsilon) _isInt_ = true;
    if (!_isInt_) return false;
}
""";
    }

    private static string GenerateMultiTypeCheck(JsonElement typeArray, string e)
    {
        var types = new List<string>();
        foreach (var item in typeArray.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                types.Add(item.GetString()!);
            }
        }

        if (types.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("    var _typeValid_ = false;");

        foreach (var type in types)
        {
            var condition = type switch
            {
                "string" => $"{e}.ValueKind == JsonValueKind.String",
                "number" => $"{e}.ValueKind == JsonValueKind.Number",
                "integer" => $"({e}.ValueKind == JsonValueKind.Number && (({e}.TryGetDecimal(out var _iv_) && _iv_ == decimal.Truncate(_iv_)) || System.Numerics.BigInteger.TryParse({e}.ToString(), System.Globalization.CultureInfo.InvariantCulture, out _) || ({e}.TryGetDouble(out var _dv_) && !double.IsInfinity(_dv_) && !double.IsNaN(_dv_) && Math.Abs(_dv_ - Math.Floor(_dv_)) < double.Epsilon)))",
                "boolean" => $"({e}.ValueKind == JsonValueKind.True || {e}.ValueKind == JsonValueKind.False)",
                "null" => $"{e}.ValueKind == JsonValueKind.Null",
                "array" => $"{e}.ValueKind == JsonValueKind.Array",
                "object" => $"{e}.ValueKind == JsonValueKind.Object",
                _ => null
            };

            if (condition != null)
            {
                sb.AppendLine($"    if ({condition}) _typeValid_ = true;");
            }
        }

        sb.AppendLine("    if (!_typeValid_) return false;");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
