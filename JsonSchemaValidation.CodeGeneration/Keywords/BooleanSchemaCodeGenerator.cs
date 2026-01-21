using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for boolean schemas (true/false).
/// </summary>
public sealed class BooleanSchemaCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "boolean-schema";
    public int Priority => 1000; // Run before everything else

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.True ||
               schema.ValueKind == JsonValueKind.False;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        // For boolean schemas, the method body is just a return statement
        // The SchemaCodeGenerator will not add "return true;" at the end if we return early
        return string.Empty;
    }

    /// <summary>
    /// Returns the complete method body for boolean schemas.
    /// </summary>
    public static string? GetBooleanSchemaBody(System.Text.Json.JsonElement schema)
    {
        if (schema.ValueKind == JsonValueKind.True)
        {
            return "return true;";
        }

        if (schema.ValueKind == JsonValueKind.False)
        {
            return "return false;";
        }

        return null;
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
