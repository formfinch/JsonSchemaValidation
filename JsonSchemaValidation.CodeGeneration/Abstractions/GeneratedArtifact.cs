// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Describes the kind of generated artifact.
/// </summary>
public enum GeneratedArtifactKind
{
    Source,
    Runtime,
    Declaration,
    Metadata,
    SourceMap,
    Other
}

/// <summary>
/// Describes how callers should treat a generated artifact.
/// </summary>
public enum GeneratedArtifactRole
{
    Primary,
    Support
}

/// <summary>
/// A generated file or text artifact produced by a target.
/// </summary>
public sealed class GeneratedArtifact
{
    public required string RelativePath { get; init; }

    public required string Content { get; init; }

    public required GeneratedArtifactKind Kind { get; init; }

    /// <summary>
    /// Indicates whether the artifact is a primary generated output or a support artifact.
    /// This can diverge from Kind when a target emits multiple primary source files,
    /// or when metadata is primary input for a downstream build step.
    /// </summary>
    public GeneratedArtifactRole Role { get; init; } = GeneratedArtifactRole.Primary;

    public string? MediaType { get; init; }
}
