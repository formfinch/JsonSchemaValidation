// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "items" keyword.
/// Draft 2019-09+: items is a single schema that applies after prefixItems.
/// Draft 3-7: items can be an array (tuple validation) or a single schema (all items).
/// </summary>
public sealed class ItemsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "items";
    public int Priority => 40;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return schema.TryGetProperty("items", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("items", out var itemsElement))
        {
            return string.Empty;
        }

        // Handle based on draft version
        if (context.DetectedDraft >= SchemaDraft.Draft201909)
        {
            return GenerateDraft202012Items(context, itemsElement);
        }
        else
        {
            return GenerateLegacyItems(context, itemsElement);
        }
    }

    /// <summary>
    /// Draft 2019-09+: items is always a single schema that applies to items after prefixItems.
    /// </summary>
    private string GenerateDraft202012Items(CodeGenerationContext context, JsonElement itemsElement)
    {
        // In 2020-12, items must be a schema (object or boolean), not an array
        if (itemsElement.ValueKind == JsonValueKind.Array)
        {
            return string.Empty; // Invalid for 2020-12
        }

        var e = context.ElementVariable;
        var eval = context.EvaluatedStateVariable;
        var loc = context.LocationVariable;
        var trackAnnotations = context.RequiresItemAnnotations;
        var hash = context.GetSubschemaHash(itemsElement);

        // Check if prefixItems exists - if so, items only validates after prefixItems
        var hasPrefixItems = context.CurrentSchema.TryGetProperty("prefixItems", out var prefixItemsElement);
        var prefixCount = 0;
        if (hasPrefixItems && prefixItemsElement.ValueKind == JsonValueKind.Array)
        {
            prefixCount = prefixItemsElement.GetArrayLength();
        }

        var sb = new StringBuilder();
        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("{");

        if (prefixCount > 0)
        {
            sb.AppendLine($"    var _itemIdx_ = 0;");
            sb.AppendLine($"    foreach (var _arrItem_ in {e}.EnumerateArray())");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (_itemIdx_ >= {prefixCount})");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (!{context.GenerateValidateCallForItem(hash, "_arrItem_", "_itemIdx_")}) return false;");
            sb.AppendLine("        }");
            sb.AppendLine("        _itemIdx_++;");
            sb.AppendLine("    }");
            if (trackAnnotations)
            {
                sb.AppendLine($"    {eval}.SetEvaluatedItemsUpTo({loc}, {e}.GetArrayLength());");
            }
        }
        else
        {
            sb.AppendLine("    var _itemIdx_ = 0;");
            sb.AppendLine($"    foreach (var _arrItem_ in {e}.EnumerateArray())");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{context.GenerateValidateCallForItem(hash, "_arrItem_", "_itemIdx_")}) return false;");
            sb.AppendLine("        _itemIdx_++;");
            sb.AppendLine("    }");
            if (trackAnnotations)
            {
                sb.AppendLine($"    {eval}.SetEvaluatedItemsUpTo({loc}, {e}.GetArrayLength());");
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Draft 3-7: items can be an array (tuple validation) or a single schema (all items).
    /// </summary>
    private string GenerateLegacyItems(CodeGenerationContext context, JsonElement itemsElement)
    {
        var e = context.ElementVariable;
        var eval = context.EvaluatedStateVariable;
        var loc = context.LocationVariable;
        var trackAnnotations = context.RequiresItemAnnotations;

        if (itemsElement.ValueKind == JsonValueKind.Array)
        {
            // Tuple validation: each schema in array validates corresponding item
            return GenerateTupleItems(context, itemsElement);
        }
        else if (itemsElement.ValueKind == JsonValueKind.Object ||
                 itemsElement.ValueKind == JsonValueKind.True ||
                 itemsElement.ValueKind == JsonValueKind.False)
        {
            // Single schema: applies to all items
            var hash = context.GetSubschemaHash(itemsElement);
            var sb = new StringBuilder();

            sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
            sb.AppendLine("{");
            sb.AppendLine("    var _itemIdx_ = 0;");
            sb.AppendLine($"    foreach (var _arrItem_ in {e}.EnumerateArray())");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{context.GenerateValidateCallForItem(hash, "_arrItem_", "_itemIdx_")}) return false;");
            sb.AppendLine("        _itemIdx_++;");
            sb.AppendLine("    }");
            if (trackAnnotations)
            {
                sb.AppendLine($"    {eval}.SetEvaluatedItemsUpTo({loc}, {e}.GetArrayLength());");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// Generate tuple validation for items array (Draft 3-7).
    /// </summary>
    private string GenerateTupleItems(CodeGenerationContext context, JsonElement itemsElement)
    {
        var e = context.ElementVariable;
        var eval = context.EvaluatedStateVariable;
        var loc = context.LocationVariable;
        var trackAnnotations = context.RequiresItemAnnotations;
        var sb = new StringBuilder();

        var itemCount = itemsElement.GetArrayLength();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("{");
        sb.AppendLine("    var _tupleIdx_ = 0;");
        sb.AppendLine($"    foreach (var _tupleItem_ in {e}.EnumerateArray())");
        sb.AppendLine("    {");

        var idx = 0;
        foreach (var subschema in itemsElement.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(subschema);
            sb.AppendLine($"        if (_tupleIdx_ == {idx})");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (!{context.GenerateValidateCallForItem(hash, "_tupleItem_", "_tupleIdx_")}) return false;");
            sb.AppendLine("        }");
            idx++;
        }

        sb.AppendLine($"        if (_tupleIdx_ >= {idx}) break;");
        sb.AppendLine("        _tupleIdx_++;");
        sb.AppendLine("    }");
        if (trackAnnotations)
        {
            sb.AppendLine($"    var _evalCount_ = Math.Min({itemCount}, {e}.GetArrayLength());");
            sb.AppendLine($"    {eval}.SetEvaluatedItemsUpTo({loc}, _evalCount_);");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}

/// <summary>
/// Generates code for the "prefixItems" keyword (Draft 2019-09+).
/// </summary>
public sealed class PrefixItemsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "prefixItems";
    public int Priority => 45; // Before items

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("prefixItems", out var arr) &&
               arr.ValueKind == JsonValueKind.Array;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        // prefixItems is Draft 2019-09+
        if (context.DetectedDraft < SchemaDraft.Draft201909)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("prefixItems", out var prefixItemsElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var eval = context.EvaluatedStateVariable;
        var loc = context.LocationVariable;
        var trackAnnotations = context.RequiresItemAnnotations;
        var sb = new StringBuilder();

        var prefixCount = prefixItemsElement.GetArrayLength();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("{");
        sb.AppendLine("    var _prefixIdx_ = 0;");
        sb.AppendLine($"    foreach (var _prefixItem_ in {e}.EnumerateArray())");
        sb.AppendLine("    {");

        var idx = 0;
        foreach (var subschema in prefixItemsElement.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(subschema);
            sb.AppendLine($"        if (_prefixIdx_ == {idx})");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (!{context.GenerateValidateCallForItem(hash, "_prefixItem_", "_prefixIdx_")}) return false;");
            sb.AppendLine("        }");
            idx++;
        }

        sb.AppendLine($"        if (_prefixIdx_ >= {idx}) break;");
        sb.AppendLine("        _prefixIdx_++;");
        sb.AppendLine("    }");
        if (trackAnnotations)
        {
            sb.AppendLine($"    var _evalCount_ = Math.Min({prefixCount}, {e}.GetArrayLength());");
            sb.AppendLine($"    {eval}.SetEvaluatedItemsUpTo({loc}, _evalCount_);");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}

/// <summary>
/// Generates code for the "additionalItems" keyword (Draft 3-7 only).
/// In these drafts, additionalItems applies to array items beyond those covered by items array.
/// </summary>
public sealed class AdditionalItemsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "additionalItems";
    public int Priority => 42; // After items tuple

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("additionalItems", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        // additionalItems only applies in Draft 3-7 (removed in 2019-09+)
        if (context.DetectedDraft >= SchemaDraft.Draft201909)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("additionalItems", out var additionalItemsElement))
        {
            return string.Empty;
        }

        // additionalItems only has effect when items is an array (tuple validation)
        if (!context.CurrentSchema.TryGetProperty("items", out var itemsElement) ||
            itemsElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var tupleLength = itemsElement.GetArrayLength();
        var e = context.ElementVariable;
        var eval = context.EvaluatedStateVariable;
        var loc = context.LocationVariable;
        var trackAnnotations = context.RequiresItemAnnotations;
        var hash = context.GetSubschemaHash(additionalItemsElement);

        var sb = new StringBuilder();
        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.Array)");
        sb.AppendLine("{");
        sb.AppendLine($"    var _addItemIdx_ = 0;");
        sb.AppendLine($"    foreach (var _addItem_ in {e}.EnumerateArray())");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (_addItemIdx_ >= {tupleLength})");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (!{context.GenerateValidateCallForItem(hash, "_addItem_", "_addItemIdx_")}) return false;");
        sb.AppendLine("        }");
        sb.AppendLine("        _addItemIdx_++;");
        sb.AppendLine("    }");
        if (trackAnnotations)
        {
            sb.AppendLine($"    {eval}.SetEvaluatedItemsUpTo({loc}, {e}.GetArrayLength());");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
