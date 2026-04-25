// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Base helper for targets with a concrete options type.
/// </summary>
/// <typeparam name="TOptions">The concrete target options type.</typeparam>
public abstract class CodeGenerationTarget<TOptions> : ICodeGenerationTarget
    where TOptions : CodeGenerationOptions
{
    public abstract CodeGenerationTargetDescriptor Descriptor { get; }

    public ValueTask<CodeGenerationCapabilityResult> GetCapabilitiesAsync(
        CodeGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        return GetCapabilitiesAsync(request, GetOptions(request), cancellationToken);
    }

    public ValueTask<CodeGenerationResult> GenerateAsync(
        CodeGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync(request, GetOptions(request), cancellationToken);
    }

    protected abstract ValueTask<CodeGenerationCapabilityResult> GetCapabilitiesAsync(
        CodeGenerationRequest request,
        TOptions options,
        CancellationToken cancellationToken);

    protected abstract ValueTask<CodeGenerationResult> GenerateAsync(
        CodeGenerationRequest request,
        TOptions options,
        CancellationToken cancellationToken);

    private static TOptions GetOptions(CodeGenerationRequest request)
    {
        if (request.Options is TOptions options)
        {
            return options;
        }

        throw new ArgumentException(
            $"Target requires options of type {typeof(TOptions).FullName}, but received {request.Options.GetType().FullName}.",
            nameof(request));
    }
}
