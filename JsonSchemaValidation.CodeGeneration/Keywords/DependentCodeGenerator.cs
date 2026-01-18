using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the legacy "dependencies" keyword (compatibility with older drafts).
/// The dependencies keyword combines both dependentRequired (array values) and dependentSchemas (schema values).
/// </summary>
public sealed class DependenciesCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "dependencies";
    public int Priority => 55;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("dependencies", out var deps) &&
               deps.ValueKind == JsonValueKind.Object;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("dependencies", out var depsElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
        sb.AppendLine("{");

        foreach (var prop in depsElement.EnumerateObject())
        {
            var triggerProp = EscapeString(prop.Name);
            sb.AppendLine($"    if ({e}.TryGetProperty(\"{triggerProp}\", out _))");
            sb.AppendLine("    {");

            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                // dependentRequired-style: array of required property names
                foreach (var required in prop.Value.EnumerateArray())
                {
                    if (required.ValueKind == JsonValueKind.String)
                    {
                        var reqProp = EscapeString(required.GetString()!);
                        sb.AppendLine($"        if (!{e}.TryGetProperty(\"{reqProp}\", out _)) return false;");
                    }
                }
            }
            else if (prop.Value.ValueKind == JsonValueKind.Object ||
                     prop.Value.ValueKind == JsonValueKind.True ||
                     prop.Value.ValueKind == JsonValueKind.False)
            {
                // dependentSchemas-style: schema that must be satisfied
                var schemaHash = context.GetSubschemaHash(prop.Value);
                sb.AppendLine($"        if (!{context.GenerateValidateCall(schemaHash)}) return false;");
            }

            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }

    private static string EscapeString(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
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
/// Generates code for the "dependentRequired" keyword.
/// </summary>
public sealed class DependentRequiredCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "dependentRequired";
    public int Priority => 55;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("dependentRequired", out var dr) &&
               dr.ValueKind == JsonValueKind.Object;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("dependentRequired", out var depReqElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
        sb.AppendLine("{");

        foreach (var prop in depReqElement.EnumerateObject())
        {
            var triggerProp = EscapeString(prop.Name);
            sb.AppendLine($"    if ({e}.TryGetProperty(\"{triggerProp}\", out _))");
            sb.AppendLine("    {");

            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var required in prop.Value.EnumerateArray())
                {
                    if (required.ValueKind == JsonValueKind.String)
                    {
                        var reqProp = EscapeString(required.GetString()!);
                        sb.AppendLine($"        if (!{e}.TryGetProperty(\"{reqProp}\", out _)) return false;");
                    }
                }
            }

            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }

    private static string EscapeString(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
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
/// Generates code for the "dependentSchemas" keyword.
/// </summary>
public sealed class DependentSchemasCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "dependentSchemas";
    public int Priority => 55;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("dependentSchemas", out var ds) &&
               ds.ValueKind == JsonValueKind.Object;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("dependentSchemas", out var depSchemasElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
        sb.AppendLine("{");

        foreach (var prop in depSchemasElement.EnumerateObject())
        {
            var triggerProp = EscapeString(prop.Name);
            var schemaHash = context.GetSubschemaHash(prop.Value);

            sb.AppendLine($"    if ({e}.TryGetProperty(\"{triggerProp}\", out _))");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{context.GenerateValidateCall(schemaHash)}) return false;");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }

    private static string EscapeString(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
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
