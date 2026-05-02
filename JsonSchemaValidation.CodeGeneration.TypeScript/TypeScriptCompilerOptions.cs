// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;

/// <summary>
/// Options passed to the TypeScript compiler wrapper.
/// </summary>
public sealed record TypeScriptCompilerOptions
{
    /// <summary>
    /// Enables TypeScript's strict type-checking profile.
    /// </summary>
    public bool Strict { get; init; }

    /// <summary>
    /// Reports an error when TypeScript would infer <c>any</c>.
    /// </summary>
    public bool NoImplicitAny { get; init; }

    /// <summary>
    /// Adds <c>undefined</c> to indexed access results.
    /// </summary>
    public bool NoUncheckedIndexedAccess { get; init; }

    /// <summary>
    /// Interprets optional property types exactly instead of adding <c>undefined</c>.
    /// </summary>
    public bool ExactOptionalPropertyTypes { get; init; }
}
