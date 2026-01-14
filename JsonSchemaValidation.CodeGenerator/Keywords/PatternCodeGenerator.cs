using System.Text.Json;

namespace JsonSchemaValidation.CodeGenerator.Keywords;

/// <summary>
/// Generates code for the "pattern" keyword.
/// </summary>
public sealed class PatternCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "pattern";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("pattern", out var pattern) &&
               pattern.ValueKind == JsonValueKind.String;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("pattern", out var patternElement))
        {
            return string.Empty;
        }

        var fieldName = $"Pattern_{context.CurrentHash}";
        var e = context.ElementVariable;

        return $$"""
if ({{e}}.ValueKind == JsonValueKind.String)
{
    var _str_ = {{e}}.GetString();
    if (_str_ != null && !{{fieldName}}.IsMatch(_str_)) return false;
}
""";
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("pattern", out var patternElement))
        {
            yield break;
        }

        var pattern = patternElement.GetString();
        if (string.IsNullOrEmpty(pattern))
        {
            yield break;
        }

        yield return new StaticFieldInfo
        {
            Type = "Regex",
            Name = $"Pattern_{context.CurrentHash}",
            Initializer = $"new Regex({EscapeStringLiteral(pattern)}, RegexOptions.Compiled)"
        };
    }

    private static string EscapeStringLiteral(string s)
    {
        return "@\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
