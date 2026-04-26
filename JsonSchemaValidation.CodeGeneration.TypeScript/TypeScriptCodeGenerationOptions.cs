// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;

/// <summary>
/// Options for the TypeScript code generation target.
/// </summary>
public sealed class TypeScriptCodeGenerationOptions : CodeGenerationOptions
{
    /// <summary>
    /// The import specifier used for the shared TypeScript runtime module.
    /// </summary>
    public string RuntimeImportSpecifier { get; init; } = "./jsv-runtime.js";

    /// <summary>
    /// Whether to assert supported "format" values for Draft 2020-12.
    /// </summary>
    public bool FormatAssertionEnabled { get; init; }

    /// <summary>
    /// Forces annotation tracking even when unevaluated* keywords are not present.
    /// </summary>
    public bool ForceAnnotationTracking { get; init; }

    /// <summary>
    /// Optional preloaded schemas keyed by absolute URI.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ExternalSchemaDocuments { get; init; }
}
