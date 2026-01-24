// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
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
        // Use () for GeneratedRegex partial methods, no () for regular Regex fields
        var regexAccess = context.UseGeneratedRegex ? $"{fieldName}()" : fieldName;

        return $$"""
if ({{e}}.ValueKind == JsonValueKind.String)
{
    var _str_ = {{e}}.GetString();
    if (_str_ != null && !{{regexAccess}}.IsMatch(_str_)) return false;
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
            Initializer = EscapeStringLiteral(transformedPattern),
            IsGeneratedRegex = true,
            RegexOptions = "RegexOptions.None",
            TimeoutMs = 5000
        };
    }

    private static string EscapeStringLiteral(string s)
    {
        return "@\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
