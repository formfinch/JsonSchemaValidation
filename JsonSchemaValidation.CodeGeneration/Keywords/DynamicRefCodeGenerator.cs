using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for "$dynamicRef" references when they can be resolved statically.
/// When $dynamicRef points to a local anchor (#anchorName), it resolves statically
/// just like $ref. Dynamic resolution only occurs when there's an outer schema
/// with a matching $dynamicAnchor in the dynamic scope.
/// </summary>
public sealed class DynamicRefCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "$dynamicRef";
    public int Priority => 199; // Run right after $ref

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!schema.TryGetProperty("$dynamicRef", out var refElement))
        {
            return false;
        }

        if (refElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var refValue = refElement.GetString();
        if (string.IsNullOrEmpty(refValue))
        {
            return false;
        }

        // Handle local references (both anchor #name and JSON pointer #/path)
        // These can be resolved statically when in the same schema resource
        return refValue.StartsWith('#');
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("$dynamicRef", out var refElement))
        {
            return string.Empty;
        }

        var refValue = refElement.GetString();
        if (string.IsNullOrEmpty(refValue))
        {
            return string.Empty;
        }

        // Only handle local references
        if (!refValue.StartsWith('#'))
        {
            return $"// $dynamicRef with external URI not supported in compiled mode: {refValue}";
        }

        // Resolve the reference (works for both anchors and JSON pointers)
        var targetSchema = context.ResolveLocalRef(refValue);
        if (!targetSchema.HasValue)
        {
            return $"// WARNING: Could not resolve $dynamicRef: {refValue}";
        }

        // Get the hash of the target schema and generate a call to its validation method
        var targetHash = context.GetSubschemaHash(targetSchema.Value);
        var e = context.ElementVariable;

        return $"// $dynamicRef: {refValue}\nif (!Validate_{targetHash}({e})) return false;";
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
