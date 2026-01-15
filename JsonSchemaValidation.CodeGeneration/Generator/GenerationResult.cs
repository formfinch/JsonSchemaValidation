namespace JsonSchemaValidation.CodeGeneration.Generator;

/// <summary>
/// Result of code generation.
/// </summary>
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
