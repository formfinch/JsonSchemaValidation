using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "enum" keyword.
/// </summary>
public sealed class EnumCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "enum";
    public int Priority => 80; // Run early after type

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("enum", out var en) &&
               en.ValueKind == JsonValueKind.Array;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("enum", out var enumElement))
        {
            return string.Empty;
        }

        var fieldName = $"Enum_{context.CurrentHash}";
        var e = context.ElementVariable;

        var sb = new StringBuilder();
        sb.AppendLine("// enum check");
        sb.AppendLine("{");
        sb.AppendLine("    var _enumValid_ = false;");
        sb.AppendLine($"    foreach (var _enumVal_ in {fieldName})");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (JsonElementDeepEquals({e}, _enumVal_))");
        sb.AppendLine("        {");
        sb.AppendLine("            _enumValid_ = true;");
        sb.AppendLine("            break;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    if (!_enumValid_) return false;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("enum", out var enumElement))
        {
            yield break;
        }

        // Create static JsonElement array for enum values
        var values = new List<string>();
        foreach (var item in enumElement.EnumerateArray())
        {
            values.Add(item.GetRawText());
        }

        if (values.Count > 0)
        {
            var arrayInit = $"new JsonElement[] {{ {string.Join(", ", values.Select(v => $"JsonDocument.Parse(\"{EscapeForString(v)}\").RootElement"))} }}";
            yield return new StaticFieldInfo
            {
                Type = "JsonElement[]",
                Name = $"Enum_{context.CurrentHash}",
                Initializer = arrayInit
            };
        }
    }

    private static string EscapeForString(string json)
    {
        var sb = new StringBuilder();
        foreach (var c in json)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\f' => "\\f",
                '\b' => "\\b",
                _ when c < 32 => $"\\u{(int)c:X4}",
                _ => c.ToString()
            });
        }
        return sb.ToString();
    }
}

/// <summary>
/// Generates code for the "const" keyword.
/// </summary>
public sealed class ConstCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "const";
    public int Priority => 80; // Run early after type

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("const", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("const", out _))
        {
            return string.Empty;
        }

        var fieldName = $"Const_{context.CurrentHash}";
        var e = context.ElementVariable;

        return $"// const check\nif (!JsonElementDeepEquals({e}, {fieldName})) return false;";
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("const", out var constElement))
        {
            yield break;
        }

        var rawJson = constElement.GetRawText();
        yield return new StaticFieldInfo
        {
            Type = "JsonElement",
            Name = $"Const_{context.CurrentHash}",
            Initializer = $"JsonDocument.Parse(\"{EscapeForString(rawJson)}\").RootElement"
        };
    }

    private static string EscapeForString(string json)
    {
        var sb = new StringBuilder();
        foreach (var c in json)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\f' => "\\f",
                '\b' => "\\b",
                _ when c < 32 => $"\\u{(int)c:X4}",
                _ => c.ToString()
            });
        }
        return sb.ToString();
    }
}
