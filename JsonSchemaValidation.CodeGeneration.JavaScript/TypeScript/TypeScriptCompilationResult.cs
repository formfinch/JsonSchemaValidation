// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.TypeScript;

/// <summary>
/// Result of invoking the TypeScript compiler.
/// </summary>
public sealed class TypeScriptCompilationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? StandardOutput { get; init; }
    public string? StandardError { get; init; }

    public static TypeScriptCompilationResult Succeeded(string? standardOutput, string? standardError)
    {
        return new TypeScriptCompilationResult
        {
            Success = true,
            StandardOutput = standardOutput,
            StandardError = standardError
        };
    }

    public static TypeScriptCompilationResult Failed(string error, string? standardOutput = null, string? standardError = null)
    {
        return new TypeScriptCompilationResult
        {
            Success = false,
            Error = error,
            StandardOutput = standardOutput,
            StandardError = standardError
        };
    }
}
