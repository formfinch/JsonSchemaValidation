// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp;

/// <summary>
/// Central contract adapter for the C# code generation target.
/// </summary>
public sealed class CSharpCodeGenerationTarget : CodeGenerationTarget<CSharpCodeGenerationOptions>
{
    public override CodeGenerationTargetDescriptor Descriptor { get; } = new()
    {
        Id = "csharp",
        DisplayName = "C#",
        SupportedFileExtensions = [".cs"],
        OptionsType = typeof(CSharpCodeGenerationOptions),
        SupportedDrafts =
        [
            SchemaDraft.Draft3,
            SchemaDraft.Draft4,
            SchemaDraft.Draft6,
            SchemaDraft.Draft7,
            SchemaDraft.Draft201909,
            SchemaDraft.Draft202012
        ]
    };

    protected override ValueTask<CodeGenerationCapabilityResult> GetCapabilitiesAsync(
        CodeGenerationRequest request,
        CSharpCodeGenerationOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var draftResult = SchemaDraftDetector.DetectDraft(request.Schema, options.DefaultDraft);
        if (!draftResult.Success)
        {
            return ValueTask.FromResult(new CodeGenerationCapabilityResult
            {
                CanGenerate = false,
                Diagnostics =
                [
                    new CodeGenerationDiagnostic
                    {
                        Severity = CodeGenerationDiagnosticSeverity.Error,
                        Code = "csharp.unsupported-draft",
                        Message = draftResult.ErrorMessage!,
                        TargetId = Descriptor.Id
                    }
                ]
            });
        }

        return ValueTask.FromResult(new CodeGenerationCapabilityResult
        {
            CanGenerate = true,
            DraftSelection = new DraftSelection
            {
                Draft = draftResult.Draft,
                Source = GetDraftSelectionSource(draftResult.Source),
                SchemaUri = draftResult.SchemaUri
            }
        });
    }

    protected override ValueTask<CodeGenerationResult> GenerateAsync(
        CodeGenerationRequest request,
        CSharpCodeGenerationOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // C# currently emits a single source artifact; EmitSupportArtifacts has no effect for this target.
        var generator = new CSharpSchemaCodeGenerator
        {
            DefaultDraft = options.DefaultDraft,
            ForceAnnotationTracking = options.ForceAnnotationTracking,
            UseGeneratedRegex = options.UseGeneratedRegex
        };

        var namespaceName = options.OutputHints.NamespaceName ?? "Generated";
        var result = generator.Generate(
            request.Schema,
            namespaceName,
            options.OutputHints.TypeName,
            options.SourcePath);

        if (!result.Success)
        {
            return ValueTask.FromResult(CodeGenerationResult.Failed(new CodeGenerationDiagnostic
            {
                Severity = CodeGenerationDiagnosticSeverity.Error,
                Code = "csharp.generation-failed",
                Message = result.Error ?? "C# code generation failed.",
                TargetId = Descriptor.Id
            }));
        }

        return ValueTask.FromResult(CodeGenerationResult.Succeeded(new GeneratedArtifact
        {
            RelativePath = result.FileName ?? $"{options.OutputHints.TypeName ?? "Validator"}.cs",
            Content = result.GeneratedCode!,
            Kind = GeneratedArtifactKind.Source,
            Role = GeneratedArtifactRole.Primary,
            MediaType = "text/x-csharp"
        }));
    }

    private static DraftSelectionSource GetDraftSelectionSource(DraftDetectionSource source)
    {
        return source switch
        {
            DraftDetectionSource.DefaultDraft => DraftSelectionSource.DefaultDraft,
            DraftDetectionSource.InferredSchemaUri => DraftSelectionSource.InferredSchemaUri,
            DraftDetectionSource.ExplicitSchemaUri => DraftSelectionSource.ExplicitSchemaUri,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown draft detection source.")
        };
    }
}
