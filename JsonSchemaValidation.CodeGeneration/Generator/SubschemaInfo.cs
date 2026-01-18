using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Generator;

/// <summary>
/// Information about a unique subschema identified during schema analysis.
/// </summary>
public sealed class SubschemaInfo
{
    /// <summary>
    /// The hash identifying this unique schema.
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// The schema JSON element.
    /// </summary>
    public required JsonElement Schema { get; init; }

    /// <summary>
    /// The generated function name for this schema.
    /// </summary>
    public string FunctionName => $"Validate_{Hash}";

    /// <summary>
    /// Whether this schema requires fallback to dynamic validators.
    /// </summary>
    public bool RequiresFallback { get; init; }

    /// <summary>
    /// Keywords that require fallback in this schema.
    /// </summary>
    public IReadOnlyList<string> FallbackKeywords { get; init; } = [];

    /// <summary>
    /// The effective base URI for this subschema (used for resolving relative $ref).
    /// This is the nearest ancestor's resolved $id, or the root schema's base URI.
    /// </summary>
    public Uri? EffectiveBaseUri { get; init; }

    /// <summary>
    /// The schema resource root for this subschema (the nearest ancestor with an $id, or the root schema).
    /// Used for resolving local JSON Pointer refs (#/$defs/...).
    /// </summary>
    public JsonElement? ResourceRoot { get; init; }

    /// <summary>
    /// The depth of the resource this subschema belongs to.
    /// Root is depth 0, nested resources with $id increment the depth.
    /// Used for $dynamicRef scope resolution.
    /// </summary>
    public int ResourceDepth { get; init; }

    /// <summary>
    /// The JSON Pointer path to this subschema from the root (e.g., "/$defs/stringArray").
    /// Null for the root schema or schemas without a canonical path.
    /// Used to register subschemas by their fragment URIs.
    /// </summary>
    public string? JsonPointerPath { get; init; }
}

/// <summary>
/// Information about a $dynamicAnchor discovered during schema analysis.
/// </summary>
public sealed class DynamicAnchorInfo
{
    /// <summary>
    /// The name of the $dynamicAnchor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The schema element that contains this $dynamicAnchor.
    /// </summary>
    public required JsonElement Schema { get; init; }

    /// <summary>
    /// The depth of the resource containing this anchor.
    /// Used to determine which anchor is "outermost" (closest to entry point).
    /// </summary>
    public required int ResourceDepth { get; init; }
}
