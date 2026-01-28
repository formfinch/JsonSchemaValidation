// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

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

    /// <summary>
    /// The names of $dynamicAnchor declarations in this subschema.
    /// Empty if no $dynamicAnchor is declared.
    /// </summary>
    public IReadOnlyList<string> DynamicAnchors { get; init; } = [];

    /// <summary>
    /// Whether this subschema has $recursiveAnchor: true.
    /// </summary>
    public bool HasRecursiveAnchor { get; init; }

    /// <summary>
    /// Whether this subschema is a schema resource root (has $id or is the root schema).
    /// Resource roots should use ResourceAnchors for scope entries.
    /// </summary>
    public bool IsResourceRoot { get; init; }

    /// <summary>
    /// All $dynamicAnchor declarations within this schema resource.
    /// Only populated for resource roots (schemas with $id or the root schema).
    /// Each entry maps anchor name to the hash of the schema containing that anchor.
    /// </summary>
    public IReadOnlyList<(string AnchorName, string SchemaHash)> ResourceAnchors { get; init; } = [];

    /// <summary>
    /// Whether this subschema should push a scope entry (has anchors).
    /// For resource roots, uses ResourceAnchors; otherwise uses direct DynamicAnchors.
    /// </summary>
    public bool ShouldPushScope => IsResourceRoot
        ? (ResourceAnchors.Count > 0 || HasRecursiveAnchor)
        : (DynamicAnchors.Count > 0 || HasRecursiveAnchor);
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
