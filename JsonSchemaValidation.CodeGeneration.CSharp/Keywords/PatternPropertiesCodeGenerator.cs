// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Draft202012.Keywords;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for the "patternProperties" keyword.
/// </summary>
public sealed class PatternPropertiesCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "patternProperties";
    public int Priority => 45; // After properties, before additionalProperties

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("patternProperties", out var pp) &&
               pp.ValueKind == JsonValueKind.Object;
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("patternProperties", out var patternPropsElement))
        {
            return string.Empty;
        }

        if (patternPropsElement.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var eval = context.EvaluatedStateVariable;
        var loc = context.LocationVariable;
        var trackAnnotations = context.RequiresPropertyAnnotations;
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
        sb.AppendLine("{");
        sb.AppendLine($"    foreach (var _ppProp_ in {e}.EnumerateObject())");
        sb.AppendLine("    {");

        var idx = 0;
        foreach (var pattern in patternPropsElement.EnumerateObject())
        {
            var fieldName = $"PatternProps_{context.CurrentHash}_{idx}";
            var schemaHash = context.GetSubschemaHash(pattern.Value);
            // Use () for GeneratedRegex partial methods, no () for regular Regex fields
            var regexAccess = context.UseGeneratedRegex ? $"{fieldName}()" : fieldName;

            sb.AppendLine($"        if ({regexAccess}.IsMatch(_ppProp_.Name))");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (!{context.GenerateValidateCallForProperty(schemaHash, "_ppProp_.Value", "_ppProp_.Name")}) return false;");
            if (trackAnnotations)
            {
                sb.AppendLine($"            {eval}.MarkPropertyEvaluated({loc}, _ppProp_.Name);");
            }
            sb.AppendLine("        }");
            idx++;
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CSharpCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("patternProperties", out var patternPropsElement))
        {
            yield break;
        }

        if (patternPropsElement.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        var idx = 0;
        foreach (var pattern in patternPropsElement.EnumerateObject())
        {
            // Use ECMAScript regex transformation for compatibility with JSON Schema spec
            var ecmaRegex = EcmaScriptRegexHelper.CreateEcmaScriptRegex(pattern.Name);
            var transformedPattern = ecmaRegex.ToString();

            yield return new StaticFieldInfo
            {
                Type = "Regex",
                Name = $"PatternProps_{context.CurrentHash}_{idx}",
                Initializer = EscapeStringLiteral(transformedPattern),
                IsGeneratedRegex = true,
                RegexOptions = "RegexOptions.None",
                TimeoutMs = 5000
            };
            idx++;
        }
    }

    private static string EscapeStringLiteral(string s)
    {
        return "@\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
