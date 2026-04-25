// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for the "required" keyword.
/// </summary>
public sealed class RequiredCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "required";
    public int Priority => 90; // Run early after type check

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("required", out var req) &&
               req.ValueKind == JsonValueKind.Array;
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("required", out var requiredElement))
        {
            return string.Empty;
        }

        if (requiredElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
        sb.AppendLine("{");

        foreach (var item in requiredElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var propName = item.GetString()!;
                var escaped = EscapeString(propName);
                sb.AppendLine($"    if (!{e}.TryGetProperty(\"{escaped}\", out _)) return false;");
            }
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
}
