using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGenerator.Keywords;

/// <summary>
/// Generates code for the "additionalProperties" keyword.
/// </summary>
public sealed class AdditionalPropertiesCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "additionalProperties";
    public int Priority => 20; // Run after properties

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("additionalProperties", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("additionalProperties", out var addPropsElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();

        // Get defined property names and pattern property patterns
        var definedProps = new HashSet<string>(StringComparer.Ordinal);
        if (context.CurrentSchema.TryGetProperty("properties", out var propsElement) &&
            propsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in propsElement.EnumerateObject())
            {
                definedProps.Add(prop.Name);
            }
        }

        var hasPatternProps = context.CurrentSchema.TryGetProperty("patternProperties", out var patternPropsElement) &&
                              patternPropsElement.ValueKind == JsonValueKind.Object;

        // Handle additionalProperties: false
        if (addPropsElement.ValueKind == JsonValueKind.False)
        {
            sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
            sb.AppendLine("{");
            sb.AppendLine($"    foreach (var _prop_ in {e}.EnumerateObject())");
            sb.AppendLine("    {");

            if (definedProps.Count > 0)
            {
                sb.AppendLine("        var _propName_ = _prop_.Name;");
                var conditions = definedProps.Select(p => $"_propName_ == \"{EscapeString(p)}\"");
                sb.AppendLine($"        if ({string.Join(" || ", conditions)}) continue;");
            }

            if (hasPatternProps)
            {
                sb.AppendLine("        var _matchesPattern_ = false;");
                foreach (var pattern in patternPropsElement.EnumerateObject())
                {
                    var patternFieldName = $"PatternProp_{context.CurrentHash}_{SanitizeName(pattern.Name)}";
                    sb.AppendLine($"        if ({patternFieldName}.IsMatch(_prop_.Name)) _matchesPattern_ = true;");
                }
                sb.AppendLine("        if (_matchesPattern_) continue;");
            }

            sb.AppendLine("        return false; // Additional property not allowed");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        // Handle additionalProperties: true (no-op, always valid)
        else if (addPropsElement.ValueKind == JsonValueKind.True)
        {
            // No code needed
            return string.Empty;
        }
        // Handle additionalProperties: schema
        else if (addPropsElement.ValueKind == JsonValueKind.Object)
        {
            var hash = context.GetSubschemaHash(addPropsElement);

            sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
            sb.AppendLine("{");
            sb.AppendLine($"    foreach (var _prop_ in {e}.EnumerateObject())");
            sb.AppendLine("    {");

            if (definedProps.Count > 0)
            {
                sb.AppendLine("        var _propName_ = _prop_.Name;");
                var conditions = definedProps.Select(p => $"_propName_ == \"{EscapeString(p)}\"");
                sb.AppendLine($"        if ({string.Join(" || ", conditions)}) continue;");
            }

            if (hasPatternProps)
            {
                sb.AppendLine("        var _matchesPattern_ = false;");
                foreach (var pattern in patternPropsElement.EnumerateObject())
                {
                    var patternFieldName = $"PatternProp_{context.CurrentHash}_{SanitizeName(pattern.Name)}";
                    sb.AppendLine($"        if ({patternFieldName}.IsMatch(_prop_.Name)) _matchesPattern_ = true;");
                }
                sb.AppendLine("        if (_matchesPattern_) continue;");
            }

            sb.AppendLine($"        if (!Validate_{hash}(_prop_.Value)) return false;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        // Generate regex fields for patternProperties if additionalProperties needs them
        if (!context.CurrentSchema.TryGetProperty("additionalProperties", out var addPropsElement))
        {
            yield break;
        }

        if (addPropsElement.ValueKind == JsonValueKind.True)
        {
            yield break;
        }

        if (!context.CurrentSchema.TryGetProperty("patternProperties", out var patternPropsElement) ||
            patternPropsElement.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var pattern in patternPropsElement.EnumerateObject())
        {
            yield return new StaticFieldInfo
            {
                Type = "Regex",
                Name = $"PatternProp_{context.CurrentHash}_{SanitizeName(pattern.Name)}",
                Initializer = $"new Regex({EscapeStringLiteral(pattern.Name)}, RegexOptions.Compiled)"
            };
        }
    }

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string EscapeStringLiteral(string s)
    {
        return "@\"" + s.Replace("\"", "\"\"") + "\"";
    }

    private static string SanitizeName(string name)
    {
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }
        return sb.ToString();
    }
}
