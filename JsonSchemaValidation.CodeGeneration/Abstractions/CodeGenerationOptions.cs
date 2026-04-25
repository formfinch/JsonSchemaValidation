// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Common options shared by all code generation targets.
/// </summary>
public class CodeGenerationOptions
{
    /// <summary>
    /// The original schema source path, when available.
    /// </summary>
    public string? SourcePath { get; init; }

    /// <summary>
    /// The draft to use when the schema does not declare $schema.
    /// </summary>
    public SchemaDraft? DefaultDraft { get; init; }

    /// <summary>
    /// Optional naming hints for generated artifacts.
    /// </summary>
    public CodeGenerationOutputHints OutputHints { get; init; } = new();

    /// <summary>
    /// Whether targets should emit runtime, declaration, or other support artifacts.
    /// </summary>
    public bool EmitSupportArtifacts { get; init; } = true;
}
