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
    public EvaluatedStateSnapshot()
    {
    }

    public IDictionary<string, HashSet<string>> EvaluatedProperties { get; } = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    public IDictionary<string, int> EvaluatedItemsUpTo { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
    public IDictionary<string, HashSet<int>> EvaluatedItemIndices { get; } = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
}
