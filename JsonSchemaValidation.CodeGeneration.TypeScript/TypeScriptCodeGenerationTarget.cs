// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;

/// <summary>
/// Central contract adapter for the TypeScript code generation target.
/// </summary>
public sealed class TypeScriptCodeGenerationTarget : CodeGenerationTarget<TypeScriptCodeGenerationOptions>
{
    public override CodeGenerationTargetDescriptor Descriptor { get; } = new()
    {
        Id = "typescript",
        DisplayName = "TypeScript",
        SupportedFileExtensions = [".ts"],
        OptionsType = typeof(TypeScriptCodeGenerationOptions),
        SupportedDrafts =
        [
            SchemaDraft.Draft4,
            SchemaDraft.Draft201909,
            SchemaDraft.Draft202012
        ]
    };

    protected override ValueTask<CodeGenerationCapabilityResult> GetCapabilitiesAsync(
        CodeGenerationRequest request,
        TypeScriptCodeGenerationOptions options,
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
                        Code = "typescript.unsupported-draft",
                        Message = draftResult.ErrorMessage!,
                        TargetId = Descriptor.Id
                    }
                ]
            });
        }

        if (!Descriptor.SupportedDrafts.Contains(draftResult.Draft))
        {
            return ValueTask.FromResult(new CodeGenerationCapabilityResult
            {
                CanGenerate = false,
                DraftSelection = CreateDraftSelection(draftResult),
                Diagnostics =
                [
                    new CodeGenerationDiagnostic
                    {
                        Severity = CodeGenerationDiagnosticSeverity.Error,
                        Code = "typescript.unsupported-draft",
                        Message = $"TypeScript target supports Draft 4, Draft 2019-09, and Draft 2020-12 only; detected {draftResult.Draft}.",
                        TargetId = Descriptor.Id
                    }
                ]
            });
        }

        var gateRejection = TsCapabilityGate.CheckSupported(request.Schema, draftResult.Draft);
        if (gateRejection != null)
        {
            return ValueTask.FromResult(new CodeGenerationCapabilityResult
            {
                CanGenerate = false,
                DraftSelection = CreateDraftSelection(draftResult),
                Diagnostics =
                [
                    new CodeGenerationDiagnostic
                    {
                        Severity = CodeGenerationDiagnosticSeverity.Error,
                        Code = "typescript.unsupported-feature",
                        Message = gateRejection,
                        TargetId = Descriptor.Id
                    }
                ]
            });
        }

        return ValueTask.FromResult(new CodeGenerationCapabilityResult
        {
            CanGenerate = true,
            DraftSelection = CreateDraftSelection(draftResult)
        });
    }

    protected override ValueTask<CodeGenerationResult> GenerateAsync(
        CodeGenerationRequest request,
        TypeScriptCodeGenerationOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var generator = new TsSchemaCodeGenerator
        {
            DefaultDraft = options.DefaultDraft,
            RuntimeImportSpecifier = options.RuntimeImportSpecifier,
            FormatAssertionEnabled = options.FormatAssertionEnabled,
            AlwaysTrackAnnotations = options.ForceAnnotationTracking,
            ExternalSchemaDocuments = options.ExternalSchemaDocuments
        };

        var result = generator.Generate(request.Schema, options.SourcePath);
        if (!result.Success)
        {
            return ValueTask.FromResult(CodeGenerationResult.Failed(new CodeGenerationDiagnostic
            {
                Severity = CodeGenerationDiagnosticSeverity.Error,
                Code = "typescript.generation-failed",
                Message = result.Error ?? "TypeScript code generation failed.",
                TargetId = Descriptor.Id
            }));
        }

        var artifacts = new List<GeneratedArtifact>
        {
            new()
            {
                RelativePath = GetPrimaryFileName(result.FileName, options.OutputHints),
                Content = result.GeneratedCode!,
                Kind = GeneratedArtifactKind.Source,
                Role = GeneratedArtifactRole.Primary,
                MediaType = "text/typescript"
            }
        };

        if (options.EmitSupportArtifacts)
        {
            string runtimeSource;
            try
            {
                runtimeSource = TsRuntime.GetSource();
            }
            catch (Exception ex)
            {
                return ValueTask.FromResult(CodeGenerationResult.Failed(new CodeGenerationDiagnostic
                {
                    Severity = CodeGenerationDiagnosticSeverity.Error,
                    Code = "typescript.runtime-generation-failed",
                    Message = $"TypeScript runtime artifact generation failed: {ex.Message}",
                    TargetId = Descriptor.Id
                }));
            }

            artifacts.Add(new GeneratedArtifact
            {
                RelativePath = TsRuntime.FileName,
                Content = runtimeSource,
                Kind = GeneratedArtifactKind.Runtime,
                Role = GeneratedArtifactRole.Support,
                MediaType = "text/typescript"
            });
        }

        return ValueTask.FromResult(CodeGenerationResult.Succeeded(artifacts.ToArray()));
    }

    private static DraftSelection CreateDraftSelection(DraftDetectionResult draftResult)
    {
        return new DraftSelection
        {
            Draft = draftResult.Draft,
            Source = GetDraftSelectionSource(draftResult.Source),
            SchemaUri = draftResult.SchemaUri
        };
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

    private static string GetPrimaryFileName(string? generatedFileName, CodeGenerationOutputHints outputHints)
    {
        var baseName = outputHints.BaseFileName ?? outputHints.ModuleName;
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return generatedFileName ?? "validator.ts";
        }

        var fileName = Path.GetFileName(baseName);
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or "..")
        {
            fileName = "validator";
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitizedCharacters = fileName.Select(c =>
            invalidCharacters.Contains(c) || c is '/' or '\\' or ':' ? '_' : c);
        fileName = new string(sanitizedCharacters.ToArray()).Trim('.', ' ');
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or "..")
        {
            fileName = "validator";
        }

        return string.Equals(Path.GetExtension(fileName), ".ts", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : Path.ChangeExtension(fileName, ".ts");
    }
}
