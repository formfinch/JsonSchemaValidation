// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;

/// <summary>
/// Target-neutral adapter contract for code generation.
/// </summary>
public interface ICodeGenerationTarget
{
    CodeGenerationTargetDescriptor Descriptor { get; }

    ValueTask<CodeGenerationCapabilityResult> GetCapabilitiesAsync(
        CodeGenerationRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<CodeGenerationResult> GenerateAsync(
        CodeGenerationRequest request,
        CancellationToken cancellationToken = default);
}
