using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for local "$ref" references.
/// External references require fallback to dynamic validators.
/// </summary>
public sealed class RefCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "$ref";
    public int Priority => 200; // Run very early - $ref replaces the entire schema in most drafts

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!schema.TryGetProperty("$ref", out var refElement))
        {
            return false;
        }

        if (refElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var refValue = refElement.GetString();

        // Only handle local references (starting with #)
        return !string.IsNullOrEmpty(refValue) && refValue.StartsWith('#');
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("$ref", out var refElement))
        {
            return string.Empty;
        }

        var refValue = refElement.GetString();
        if (string.IsNullOrEmpty(refValue) || !refValue.StartsWith('#'))
        {
            return string.Empty;
        }

        // Resolve the $ref to get the target schema
        var targetSchema = context.ResolveLocalRef(refValue);
        if (!targetSchema.HasValue)
        {
            // Cannot resolve - this shouldn't happen if SubschemaExtractor did its job
            return $"// WARNING: Could not resolve $ref: {refValue}";
        }

        // Get the hash of the target schema and generate a call to its validation method
        var targetHash = context.GetSubschemaHash(targetSchema.Value);
        var e = context.ElementVariable;

        return $"// $ref: {refValue}\nif (!Validate_{targetHash}({e})) return false;";
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
