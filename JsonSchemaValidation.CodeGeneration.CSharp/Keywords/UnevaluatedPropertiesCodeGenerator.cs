// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for the "unevaluatedProperties" keyword.
/// This keyword validates properties that were not evaluated by properties,
/// patternProperties, or additionalProperties in the current or any applied subschema.
/// </summary>
public sealed class UnevaluatedPropertiesCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "unevaluatedProperties";
    public int Priority => -100; // Run last, after all property-evaluating keywords

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("unevaluatedProperties", out _);
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
    {
        // unevaluatedProperties was introduced in Draft 2019-09
        if (context.DetectedDraft < SchemaDraft.Draft201909)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("unevaluatedProperties", out var unevalPropsElement))
        {
            return string.Empty;
        }

        if (!context.RequiresPropertyAnnotations)
        {
            return "// WARNING: unevaluatedProperties requires annotation tracking";
        }

        var e = context.ElementVariable;
        var eval = context.EvaluatedStateVariable;
        var loc = context.LocationVariable;
        var sb = new StringBuilder();

        // Handle unevaluatedProperties: false
        if (unevalPropsElement.ValueKind == JsonValueKind.False)
        {
            sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
            sb.AppendLine("{");
            sb.AppendLine($"    foreach (var _prop_ in {e}.EnumerateObject())");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{eval}.IsPropertyEvaluated({loc}, _prop_.Name))");
            sb.AppendLine("        {");
            sb.AppendLine("            return false; // Unevaluated property not allowed");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        // Handle unevaluatedProperties: true (no-op, but mark as evaluated)
        else if (unevalPropsElement.ValueKind == JsonValueKind.True)
        {
            sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
            sb.AppendLine("{");
            sb.AppendLine($"    foreach (var _prop_ in {e}.EnumerateObject())");
            sb.AppendLine("    {");
            sb.AppendLine($"        {eval}.MarkPropertyEvaluated({loc}, _prop_.Name);");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        // Handle unevaluatedProperties: schema
        else if (unevalPropsElement.ValueKind == JsonValueKind.Object)
        {
            var hash = context.GetSubschemaHash(unevalPropsElement);

            sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Object)");
            sb.AppendLine("{");
            sb.AppendLine($"    foreach (var _prop_ in {e}.EnumerateObject())");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{eval}.IsPropertyEvaluated({loc}, _prop_.Name))");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (!{context.GenerateValidateCallForProperty(hash, "_prop_.Value", "_prop_.Name")}) return false;");
            sb.AppendLine($"            {eval}.MarkPropertyEvaluated({loc}, _prop_.Name);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CSharpCodeGenerationContext context)
    {
        return [];
    }
}
