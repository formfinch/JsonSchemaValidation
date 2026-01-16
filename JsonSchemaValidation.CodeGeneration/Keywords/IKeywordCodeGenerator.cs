using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

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
    /// Function to resolve a local $ref (e.g., "#/$defs/foo") to the target schema.
    /// Returns null if the reference cannot be resolved.
    /// </summary>
    public required Func<string, JsonElement?> ResolveLocalRef { get; init; }

    /// <summary>
    /// The variable name for the element being validated (usually "e").
    /// </summary>
    public string ElementVariable { get; init; } = "e";

    /// <summary>
    /// The base URI of the schema being compiled (used to resolve relative $ref).
    /// </summary>
    public Uri? BaseUri { get; init; }

    /// <summary>
    /// Collection of external $ref URIs that need to be resolved from the registry.
    /// Populated during code generation, consumed by SchemaCodeGenerator for field generation.
    /// </summary>
    public List<ExternalRefInfo> ExternalRefs { get; init; } = [];
}

/// <summary>
/// Information about an external $ref that needs to be resolved from the registry.
/// </summary>
public sealed class ExternalRefInfo
{
    /// <summary>
    /// The field name to use for caching this external validator.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// The target URI of the external $ref (absolute, with fragment if present).
    /// </summary>
    public required Uri TargetUri { get; init; }

    /// <summary>
    /// The original $ref value as it appeared in the schema.
    /// </summary>
    public required string OriginalRef { get; init; }
}
