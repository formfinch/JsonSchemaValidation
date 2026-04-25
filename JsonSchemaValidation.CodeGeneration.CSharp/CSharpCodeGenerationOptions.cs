// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp;

/// <summary>
/// Options for the C# code generation target.
/// </summary>
public sealed class CSharpCodeGenerationOptions : CodeGenerationOptions
{
    /// <summary>
    /// Whether to use [GeneratedRegex] partial methods instead of regular Regex fields.
    /// </summary>
    public bool UseGeneratedRegex { get; init; }

    /// <summary>
    /// Forces annotation tracking even when unevaluated* keywords are not present.
    /// </summary>
    public bool ForceAnnotationTracking { get; init; }
}
