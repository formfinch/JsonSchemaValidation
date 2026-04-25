// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Severity for code generation diagnostics.
/// </summary>
public enum CodeGenerationDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Caller-facing diagnostic reported by capability checks or generation.
/// </summary>
public sealed class CodeGenerationDiagnostic
{
    public required CodeGenerationDiagnosticSeverity Severity { get; init; }

    public string? Code { get; init; }

    public required string Message { get; init; }

    public string? JsonPointer { get; init; }

    public string? TargetId { get; init; }
}
