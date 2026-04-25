// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Details for a schema feature that a target cannot generate.
/// </summary>
public sealed class UnsupportedFeatureDetail
{
    public required string TargetId { get; init; }

    public required string FeatureName { get; init; }

    public string? JsonPointer { get; init; }

    public SchemaDraft? Draft { get; init; }

    public required string Reason { get; init; }
}
