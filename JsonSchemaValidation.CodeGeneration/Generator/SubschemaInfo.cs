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
}
