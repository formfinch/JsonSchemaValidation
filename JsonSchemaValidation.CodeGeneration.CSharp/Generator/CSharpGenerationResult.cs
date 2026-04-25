// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;

/// <summary>
/// Single-source result returned by the C# schema generator facade.
/// </summary>
public sealed class CSharpGenerationResult
{
    public bool Success { get; init; }

    public string? GeneratedCode { get; init; }

    public string? FileName { get; init; }

    public string? Error { get; init; }

    public static CSharpGenerationResult Succeeded(string generatedCode, string fileName)
    {
        return new CSharpGenerationResult
        {
            Success = true,
            GeneratedCode = generatedCode,
            FileName = fileName
        };
    }

    public static CSharpGenerationResult Failed(string error)
    {
        return new CSharpGenerationResult
        {
            Success = false,
            Error = error
        };
    }
}
