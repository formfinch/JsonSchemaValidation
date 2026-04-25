// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for the "properties" keyword.
/// </summary>
public sealed class PropertiesCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "properties";
    public int Priority => 50; // Standard priority for applicators

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("properties", out var props) &&
               props.ValueKind == JsonValueKind.Object;
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
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
        var eval = context.EvaluatedStateVariable;
        var loc = context.LocationVariable;
        var trackAnnotations = context.RequiresPropertyAnnotations;
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
        sb.AppendLine("{");

        var idx = 0;
        foreach (var prop in propertiesElement.EnumerateObject())
        {
            var propName = prop.Name;
            var escaped = EscapeString(propName);
            var propHash = context.GetSubschemaHash(prop.Value);
            var varName = $"prop{idx}";

            // Check for Draft 3's "required: true" on the property schema (Draft 3 only)
            var hasDraft3Required = context.DetectedDraft == SchemaDraft.Draft3 &&
                                    prop.Value.ValueKind == JsonValueKind.Object &&
                                    prop.Value.TryGetProperty("required", out var reqElement) &&
                                    reqElement.ValueKind == JsonValueKind.True;

            sb.AppendLine($"    if ({e}.TryGetProperty(\"{escaped}\", out var _{varName}_))");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{context.GenerateValidateCallForProperty(propHash, $"_{varName}_", $"\"{escaped}\"")}) return false;");
            if (trackAnnotations)
            {
                sb.AppendLine($"        {eval}.MarkPropertyEvaluated({loc}, \"{escaped}\");");
            }
            sb.AppendLine("    }");

            // Draft 3: if "required: true" on property schema, the property must exist
            if (hasDraft3Required)
            {
                sb.AppendLine("    else");
                sb.AppendLine("    {");
                sb.AppendLine($"        return false; // Draft 3: required property \"{escaped}\" is missing");
                sb.AppendLine("    }");
            }

            idx++;
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CSharpCodeGenerationContext context)
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
