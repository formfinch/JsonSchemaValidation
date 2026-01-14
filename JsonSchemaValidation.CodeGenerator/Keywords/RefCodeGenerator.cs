using System.Text.Json;

namespace JsonSchemaValidation.CodeGenerator.Keywords;

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
        // $ref handling is done at schema resolution time, not code generation time
        // The referenced schema should already be resolved and have its own Validate_ function
        // This generator is a placeholder - actual $ref resolution happens in SubschemaExtractor

        // For now, we don't generate any code here because $ref schemas
        // should be resolved to their target schemas during extraction
        return string.Empty;
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
