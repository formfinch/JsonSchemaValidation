// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Metadata describing a code generation target.
/// </summary>
public sealed class CodeGenerationTargetDescriptor
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public IReadOnlyList<string> SupportedFileExtensions { get; init; } = [];

    public required Type OptionsType { get; init; }

    public IReadOnlyList<SchemaDraft> SupportedDrafts { get; init; } = [];
}
