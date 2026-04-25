// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Request passed to a code generation target.
/// </summary>
public sealed class CodeGenerationRequest
{
    /// <summary>
    /// The parsed schema to generate code from.
    /// </summary>
    public required JsonElement Schema { get; init; }

    /// <summary>
    /// Target-specific options derived from common code generation options.
    /// </summary>
    public required CodeGenerationOptions Options { get; init; }
}
