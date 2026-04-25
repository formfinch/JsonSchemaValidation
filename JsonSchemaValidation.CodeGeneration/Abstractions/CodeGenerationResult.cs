// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Multi-artifact result returned by a code generation target.
/// </summary>
public sealed class CodeGenerationResult
{
    public bool Success { get; init; }

    public IReadOnlyList<GeneratedArtifact> Artifacts { get; init; } = [];

    public IReadOnlyList<CodeGenerationDiagnostic> Diagnostics { get; init; } = [];

    public static CodeGenerationResult Succeeded(params GeneratedArtifact[] artifacts)
    {
        return new CodeGenerationResult
        {
            Success = true,
            Artifacts = artifacts
        };
    }

    public static CodeGenerationResult Failed(params CodeGenerationDiagnostic[] diagnostics)
    {
        return new CodeGenerationResult
        {
            Success = false,
            Diagnostics = diagnostics
        };
    }
}
