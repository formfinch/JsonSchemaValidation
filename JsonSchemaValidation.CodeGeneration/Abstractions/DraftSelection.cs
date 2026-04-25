// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Describes how the effective schema draft was selected.
/// </summary>
public sealed class DraftSelection
{
    public required SchemaDraft Draft { get; init; }

    public required DraftSelectionSource Source { get; init; }

    public string? SchemaUri { get; init; }

    public string? Reason { get; init; }
}

public enum DraftSelectionSource
{
    ExplicitSchemaUri,
    DefaultDraft,
    InferredSchemaUri
}
