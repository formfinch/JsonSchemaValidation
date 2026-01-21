// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "unevaluatedItems" keyword.
/// This keyword validates array items that were not evaluated by prefixItems,
/// items, or contains in the current or any applied subschema.
/// </summary>
public sealed class UnevaluatedItemsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "unevaluatedItems";
    public int Priority => -100; // Run last, after all item-evaluating keywords

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("unevaluatedItems", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("unevaluatedItems", out var unevalItemsElement))
        {
            return string.Empty;
        }

        if (!context.RequiresItemAnnotations)
        {
            return "// WARNING: unevaluatedItems requires annotation tracking";
        }

        var e = context.ElementVariable;
        var eval = context.EvaluatedStateVariable;
        var loc = context.LocationVariable;
        var sb = new StringBuilder();

        // Handle unevaluatedItems: false
        if (unevalItemsElement.ValueKind == JsonValueKind.False)
        {
            sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
            sb.AppendLine("{");
            sb.AppendLine("    var _unevalIdx_ = 0;");
            sb.AppendLine($"    foreach (var _item_ in {e}.EnumerateArray())");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{eval}.IsItemEvaluated({loc}, _unevalIdx_))");
            sb.AppendLine("        {");
            sb.AppendLine("            return false; // Unevaluated item not allowed");
            sb.AppendLine("        }");
            sb.AppendLine("        _unevalIdx_++;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        // Handle unevaluatedItems: true (no-op, but mark as evaluated)
        else if (unevalItemsElement.ValueKind == JsonValueKind.True)
        {
            sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
            sb.AppendLine("{");
            sb.AppendLine($"    {eval}.SetEvaluatedItemsUpTo({loc}, {e}.GetArrayLength());");
            sb.AppendLine("}");
        }
        // Handle unevaluatedItems: schema
        else if (unevalItemsElement.ValueKind == JsonValueKind.Object)
        {
            var hash = context.GetSubschemaHash(unevalItemsElement);

            sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
            sb.AppendLine("{");
            sb.AppendLine("    var _unevalIdx_ = 0;");
            sb.AppendLine($"    foreach (var _item_ in {e}.EnumerateArray())");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{eval}.IsItemEvaluated({loc}, _unevalIdx_))");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (!{context.GenerateValidateCallForItem(hash, "_item_", "_unevalIdx_")}) return false;");
            sb.AppendLine("        }");
            sb.AppendLine("        _unevalIdx_++;");
            sb.AppendLine("    }");
            sb.AppendLine($"    {eval}.SetEvaluatedItemsUpTo({loc}, {e}.GetArrayLength());");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
