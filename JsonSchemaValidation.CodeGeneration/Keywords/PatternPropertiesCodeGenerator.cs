using System.Text;
using System.Text.Json;
using JsonSchemaValidation.Draft202012.Keywords;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "patternProperties" keyword.
/// </summary>
public sealed class PatternPropertiesCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "patternProperties";
    public int Priority => 45; // After properties, before additionalProperties

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("patternProperties", out var pp) &&
               pp.ValueKind == JsonValueKind.Object;
    }

    public string GenerateCode(CodeGenerationContext context)
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

            sb.AppendLine($"        if ({fieldName}.IsMatch(_ppProp_.Name))");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (!Validate_{schemaHash}(_ppProp_.Value)) return false;");
            if (trackAnnotations)
            {
                sb.AppendLine($"            {eval}.EvaluatedProperties.Add(_ppProp_.Name);");
            }
            sb.AppendLine("        }");
            idx++;
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
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
                Initializer = $"new Regex({EscapeStringLiteral(transformedPattern)}, RegexOptions.Compiled, TimeSpan.FromSeconds(5))"
            };
            idx++;
        }
    }

    private static string EscapeStringLiteral(string s)
    {
        return "@\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
