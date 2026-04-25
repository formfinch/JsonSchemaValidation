// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Result of checking whether a target can generate code for a schema.
/// </summary>
public sealed class CodeGenerationCapabilityResult
{
    public bool CanGenerate { get; init; }

    public DraftSelection? DraftSelection { get; init; }

    public IReadOnlyList<CodeGenerationDiagnostic> Diagnostics { get; init; } = [];

    public IReadOnlyList<UnsupportedDraftDetail> UnsupportedDrafts { get; init; } = [];

    public IReadOnlyList<UnsupportedFeatureDetail> UnsupportedFeatures { get; init; } = [];
}
