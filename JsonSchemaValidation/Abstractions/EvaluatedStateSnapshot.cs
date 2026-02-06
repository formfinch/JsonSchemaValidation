// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;

namespace FormFinch.JsonSchemaValidation.Abstractions;

/// <summary>
/// Snapshot of evaluated properties/items annotations for compiled validators.
/// Used to merge evaluation state across external $ref boundaries.
/// </summary>
public sealed class EvaluatedStateSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluatedStateSnapshot"/> class with empty collections.
    /// </summary>
    public EvaluatedStateSnapshot()
    {
    }

    /// <summary>
    /// Gets the evaluated properties per instance path. Keys are JSON Pointer instance paths;
    /// values are the set of property names evaluated at that path.
    /// </summary>
    public IDictionary<string, HashSet<string>> EvaluatedProperties { get; } = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the highest evaluated array index per instance path (exclusive upper bound for prefix-based evaluation).
    /// </summary>
    public IDictionary<string, int> EvaluatedItemsUpTo { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the individually evaluated array indices per instance path (for non-contiguous item evaluation).
    /// </summary>
    public IDictionary<string, HashSet<int>> EvaluatedItemIndices { get; } = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
}
