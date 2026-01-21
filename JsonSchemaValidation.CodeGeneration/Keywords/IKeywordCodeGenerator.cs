using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

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
    /// Function to resolve a URI to an internal schema that has a matching $id.
    /// Returns null if no internal schema has the given $id.
    /// </summary>
    public required Func<string, JsonElement?> ResolveInternalId { get; init; }

    /// <summary>
    /// Function to resolve a local $ref within a specific schema resource.
    /// </summary>
    public required Func<string, JsonElement, JsonElement?> ResolveLocalRefInResource { get; init; }

    /// <summary>
    /// The schema resource root for the current subschema (nearest ancestor with $id, or root schema).
    /// Used for resolving local JSON Pointer refs (#/$defs/...).
    /// </summary>
    public JsonElement? ResourceRoot { get; init; }

    /// <summary>
    /// The depth of the resource containing the current subschema.
    /// Used for $dynamicRef scope resolution.
    /// </summary>
    public int ResourceDepth { get; init; }

    /// <summary>
    /// Function to find the outermost $dynamicAnchor with the given name.
    /// </summary>
    public Func<string, JsonElement?>? FindOutermostDynamicAnchor { get; init; }

    /// <summary>
    /// Function to find an outer $dynamicAnchor (at lower depth than current resource).
    /// </summary>
    public Func<string, int, JsonElement?>? FindOuterDynamicAnchor { get; init; }

    /// <summary>
    /// The variable name for the element being validated (usually "e").
    /// </summary>
    public string ElementVariable { get; init; } = "e";

    /// <summary>
    /// The effective base URI for this subschema (used to resolve relative $ref).
    /// This may differ from RootBaseUri when the subschema has its own $id.
    /// </summary>
    public Uri? BaseUri { get; init; }

    /// <summary>
    /// The root schema's base URI (used for self-reference detection).
    /// </summary>
    public Uri? RootBaseUri { get; init; }

    /// <summary>
    /// Collection of external $ref URIs that need to be resolved from the registry.
    /// Populated during code generation, consumed by SchemaCodeGenerator for field generation.
    /// </summary>
    public List<ExternalRefInfo> ExternalRefs { get; init; } = [];

    /// <summary>
    /// Whether the schema tree contains unevaluatedProperties.
    /// When true, property evaluation must be tracked.
    /// </summary>
    public bool RequiresPropertyAnnotations { get; init; }

    /// <summary>
    /// Whether the schema tree contains unevaluatedItems.
    /// When true, item evaluation must be tracked.
    /// </summary>
    public bool RequiresItemAnnotations { get; init; }

    /// <summary>
    /// The variable name for the evaluated state (e.g., "_eval_").
    /// Only used when RequiresPropertyAnnotations or RequiresItemAnnotations is true.
    /// </summary>
    public string EvaluatedStateVariable { get; init; } = "_eval_";

    /// <summary>
    /// The variable name for the current instance location (e.g., "_loc_").
    /// Only used when RequiresPropertyAnnotations or RequiresItemAnnotations is true.
    /// </summary>
    public string LocationVariable { get; init; } = "_loc_";

    /// <summary>
    /// Whether annotation tracking requires passing location through method calls.
    /// </summary>
    public bool RequiresLocationTracking => RequiresPropertyAnnotations || RequiresItemAnnotations;

    /// <summary>
    /// Gets the location argument suffix for method calls (", _loc_" if location tracking is enabled, empty otherwise).
    /// </summary>
    public string LocationArgument => RequiresLocationTracking ? $", {LocationVariable}" : "";

    /// <summary>
    /// Generates a method call to validate a subschema, passing location if needed.
    /// For same-level calls (allOf, anyOf, etc.)
    /// </summary>
    public string GenerateValidateCall(string hash) =>
        RequiresLocationTracking ? $"Validate_{hash}({ElementVariable}, {LocationVariable})" : $"Validate_{hash}({ElementVariable})";

    /// <summary>
    /// Generates a method call to validate a subschema with a different element variable.
    /// For property validation where we're validating a property value.
    /// </summary>
    public string GenerateValidateCallForVariable(string hash, string variable) =>
        RequiresLocationTracking ? $"Validate_{hash}({variable}, {LocationVariable})" : $"Validate_{hash}({variable})";

    /// <summary>
    /// Generates a method call to validate a child property (pushes property name onto location).
    /// </summary>
    public string GenerateValidateCallForProperty(string hash, string propertyValueVar, string propertyNameLiteral) =>
        RequiresLocationTracking
            ? $"Validate_{hash}({propertyValueVar}, {LocationVariable} + \"/\" + EscapeJsonPointer({propertyNameLiteral}))"
            : $"Validate_{hash}({propertyValueVar})";

    /// <summary>
    /// Generates a method call to validate a child array item (pushes index onto location).
    /// </summary>
    public string GenerateValidateCallForItem(string hash, string itemVar, string indexVar) =>
        RequiresLocationTracking
            ? $"Validate_{hash}({itemVar}, {LocationVariable} + \"/\" + {indexVar})"
            : $"Validate_{hash}({itemVar})";
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
