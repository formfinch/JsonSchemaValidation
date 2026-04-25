// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for the "propertyNames" keyword.
/// </summary>
public sealed class PropertyNamesCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "propertyNames";
    public int Priority => 60;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("propertyNames", out _);
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
    {
        // propertyNames was introduced in Draft 6
        if (context.DetectedDraft < SchemaDraft.Draft6)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("propertyNames", out var propNamesElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var schemaHash = context.GetSubschemaHash(propNamesElement);
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
        sb.AppendLine("{");
        sb.AppendLine($"    foreach (var _pnProp_ in {e}.EnumerateObject())");
        sb.AppendLine("    {");
        sb.AppendLine("        using var _pnDoc_ = JsonDocument.Parse($\"\\\"{_pnProp_.Name}\\\"\");");
        sb.AppendLine($"        if (!{context.GenerateValidateCallForVariable(schemaHash, "_pnDoc_.RootElement")}) return false;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CSharpCodeGenerationContext context)
    {
        return [];
    }
}
