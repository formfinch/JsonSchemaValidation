// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Target-neutral hints for naming generated artifacts.
/// </summary>
public sealed class CodeGenerationOutputHints
{
    /// <summary>
    /// Suggested base file name without extension.
    /// </summary>
    public string? BaseFileName { get; init; }

    /// <summary>
    /// Suggested namespace for targets that use namespaces.
    /// </summary>
    public string? NamespaceName { get; init; }

    /// <summary>
    /// Suggested generated type name for targets that emit types.
    /// </summary>
    public string? TypeName { get; init; }

    /// <summary>
    /// Suggested module name for targets that emit modules.
    /// </summary>
    public string? ModuleName { get; init; }
}
