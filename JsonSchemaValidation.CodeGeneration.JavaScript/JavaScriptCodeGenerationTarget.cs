// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript;

/// <summary>
/// Central contract adapter for the direct JavaScript code generation target.
/// </summary>
public sealed class JavaScriptCodeGenerationTarget : CodeGenerationTarget<JavaScriptCodeGenerationOptions>
{
    public override CodeGenerationTargetDescriptor Descriptor { get; } = new()
    {
        Id = "javascript",
        DisplayName = "JavaScript",
        SupportedFileExtensions = [".js"],
        OptionsType = typeof(JavaScriptCodeGenerationOptions),
        SupportedDrafts =
        [
            SchemaDraft.Draft4,
            SchemaDraft.Draft201909,
            SchemaDraft.Draft202012
        ]
    };

    protected override ValueTask<CodeGenerationCapabilityResult> GetCapabilitiesAsync(
        CodeGenerationRequest request,
        JavaScriptCodeGenerationOptions options,
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
                        Code = "javascript.unsupported-draft",
                        Message = draftResult.ErrorMessage!,
                        TargetId = Descriptor.Id
                    }
                ]
            });
        }

        var draftSelection = CreateDraftSelection(draftResult);
        if (!Descriptor.SupportedDrafts.Contains(draftResult.Draft))
        {
            var message = $"JavaScript target supports Draft 4, Draft 2019-09, and Draft 2020-12 only; detected {draftResult.Draft}.";
            return ValueTask.FromResult(new CodeGenerationCapabilityResult
            {
                CanGenerate = false,
                DraftSelection = draftSelection,
                Diagnostics =
                [
                    new CodeGenerationDiagnostic
                    {
                        Severity = CodeGenerationDiagnosticSeverity.Error,
                        Code = "javascript.unsupported-draft",
                        Message = message,
                        TargetId = Descriptor.Id
                    }
                ],
                UnsupportedDrafts =
                [
                    new UnsupportedDraftDetail
                    {
                        TargetId = Descriptor.Id,
                        Draft = draftResult.Draft,
                        Reason = message
                    }
                ]
            });
        }

        var gateRejection = JsCapabilityGate.CheckSupport(request.Schema, draftResult.Draft);
        if (gateRejection != null)
        {
            return ValueTask.FromResult(new CodeGenerationCapabilityResult
            {
                CanGenerate = false,
                DraftSelection = draftSelection,
                Diagnostics =
                [
                    new CodeGenerationDiagnostic
                    {
                        Severity = CodeGenerationDiagnosticSeverity.Error,
                        Code = "javascript.unsupported-feature",
                        Message = gateRejection.Reason,
                        JsonPointer = gateRejection.JsonPointer,
                        TargetId = Descriptor.Id
                    }
                ],
                UnsupportedFeatures =
                [
                    new UnsupportedFeatureDetail
                    {
                        TargetId = Descriptor.Id,
                        FeatureName = gateRejection.FeatureName,
                        JsonPointer = gateRejection.JsonPointer,
                        Draft = draftResult.Draft,
                        Reason = gateRejection.Reason
                    }
                ]
            });
        }

        return ValueTask.FromResult(new CodeGenerationCapabilityResult
        {
            CanGenerate = true,
            DraftSelection = draftSelection
        });
    }

    protected override ValueTask<CodeGenerationResult> GenerateAsync(
        CodeGenerationRequest request,
        JavaScriptCodeGenerationOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var generator = new JsSchemaCodeGenerator
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
                Code = "javascript.generation-failed",
                Message = result.Error ?? "JavaScript code generation failed.",
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
                MediaType = "text/javascript"
            }
        };

        if (options.EmitSupportArtifacts)
        {
            string runtimeSource;
            try
            {
                runtimeSource = JsRuntime.GetSource();
            }
            catch (Exception ex)
            {
                return ValueTask.FromResult(CodeGenerationResult.Failed(new CodeGenerationDiagnostic
                {
                    Severity = CodeGenerationDiagnosticSeverity.Error,
                    Code = "javascript.runtime-generation-failed",
                    Message = $"JavaScript runtime artifact generation failed: {ex.Message}",
                    TargetId = Descriptor.Id
                }));
            }

            artifacts.Add(new GeneratedArtifact
            {
                RelativePath = JsRuntime.FileName,
                Content = runtimeSource,
                Kind = GeneratedArtifactKind.Runtime,
                Role = GeneratedArtifactRole.Support,
                MediaType = "text/javascript"
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
            return generatedFileName ?? "validator.js";
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

        return string.Equals(Path.GetExtension(fileName), ".js", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : Path.ChangeExtension(fileName, ".js");
    }

}
