// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

/// <summary>
/// Result of code generation.
/// </summary>
[Obsolete("Use FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions.CodeGenerationResult; will be removed in #38.")]
public sealed class GenerationResult
{
    public bool Success { get; init; }
    public string? GeneratedCode { get; init; }
    public string? FileName { get; init; }
    public string? Error { get; init; }

    public static GenerationResult Succeeded(string generatedCode, string fileName)
    {
        return new GenerationResult
        {
            Success = true,
            GeneratedCode = generatedCode,
            FileName = fileName
        };
    }

    public static GenerationResult Failed(string error)
    {
        return new GenerationResult
        {
            Success = false,
            Error = error
        };
    }
}
