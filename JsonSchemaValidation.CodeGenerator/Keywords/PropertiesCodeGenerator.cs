using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGenerator.Keywords;

/// <summary>
/// Generates code for the "properties" keyword.
/// </summary>
public sealed class PropertiesCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "properties";
    public int Priority => 50; // Standard priority for applicators

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("properties", out var props) &&
               props.ValueKind == JsonValueKind.Object;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("properties", out var propertiesElement))
        {
            return string.Empty;
        }

        if (propertiesElement.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
        sb.AppendLine("{");

        foreach (var prop in propertiesElement.EnumerateObject())
        {
            var propName = prop.Name;
            var escaped = EscapeString(propName);
            var propHash = context.GetSubschemaHash(prop.Value);
            var varName = SanitizeVariableName(propName);

            sb.AppendLine($"    if ({e}.TryGetProperty(\"{escaped}\", out var _{varName}_))");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!Validate_{propHash}(_{varName}_)) return false;");
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
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string SanitizeVariableName(string name)
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
