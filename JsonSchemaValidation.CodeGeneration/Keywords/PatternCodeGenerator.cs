using System.Text.Json;
using FormFinch.JsonSchemaValidation.Draft202012.Keywords;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "pattern" keyword.
/// Uses ECMAScript-compatible regex transformation matching the dynamic validator.
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

        // Use ECMAScript regex transformation for compatibility with JSON Schema spec
        // This transforms \d, \w, \s etc. to ECMAScript-compatible equivalents
        var ecmaRegex = EcmaScriptRegexHelper.CreateEcmaScriptRegex(pattern);
        var transformedPattern = ecmaRegex.ToString();

        yield return new StaticFieldInfo
        {
            Type = "Regex",
            Name = $"Pattern_{context.CurrentHash}",
            Initializer = $"new Regex({EscapeStringLiteral(transformedPattern)}, RegexOptions.Compiled, TimeSpan.FromSeconds(5))"
        };
    }

    private static string EscapeStringLiteral(string s)
    {
        return "@\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
