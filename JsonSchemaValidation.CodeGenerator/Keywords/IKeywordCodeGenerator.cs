using System.Text.Json;

namespace JsonSchemaValidation.CodeGenerator.Keywords;

/// <summary>
/// Interface for keyword-specific code generators.
/// </summary>
public interface IKeywordCodeGenerator
{
    /// <summary>
    /// The JSON Schema keyword this generator handles.
    /// </summary>
    string Keyword { get; }

    /// <summary>
    /// Priority for code generation. Higher values run first.
    /// Type checks should run first (100), then constraints (50), then applicators (0).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Returns true if this generator can handle the keyword in the given schema.
    /// </summary>
    bool CanGenerate(JsonElement schema);

    /// <summary>
    /// Generates validation code for the keyword.
    /// </summary>
    /// <param name="context">Code generation context.</param>
    /// <returns>C# code snippet for validation.</returns>
    string GenerateCode(CodeGenerationContext context);

    /// <summary>
    /// Returns any static field declarations needed (e.g., compiled Regex).
    /// </summary>
    IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context);
}

/// <summary>
/// Information about a static field to be declared in the generated class.
/// </summary>
public sealed class StaticFieldInfo
{
    public required string Type { get; init; }
    public required string Name { get; init; }
    public required string Initializer { get; init; }
}

/// <summary>
/// Context provided to keyword code generators.
/// </summary>
public sealed class CodeGenerationContext
{
    /// <summary>
    /// The current schema being processed.
    /// </summary>
    public required JsonElement CurrentSchema { get; init; }

    /// <summary>
    /// The hash of the current schema.
    /// </summary>
    public required string CurrentHash { get; init; }

    /// <summary>
    /// Function to get the hash for a subschema.
    /// </summary>
    public required Func<JsonElement, string> GetSubschemaHash { get; init; }

    /// <summary>
    /// The variable name for the element being validated (usually "e").
    /// </summary>
    public string ElementVariable { get; init; } = "e";
}
