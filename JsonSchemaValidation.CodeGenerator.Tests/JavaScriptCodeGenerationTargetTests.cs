// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public sealed class JavaScriptCodeGenerationTargetTests
{
    [Fact]
    public async Task GetCapabilitiesAsync_SupportedSchema_ReturnsDraftSelection()
    {
        using var doc = JsonDocument.Parse("""{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"string"}""");
        var target = new JavaScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new JavaScriptCodeGenerationOptions()
        };

        var result = await target.GetCapabilitiesAsync(request);

        Assert.True(result.CanGenerate);
        Assert.NotNull(result.DraftSelection);
        Assert.Equal("javascript", target.Descriptor.Id);
        Assert.Equal(typeof(JavaScriptCodeGenerationOptions), target.Descriptor.OptionsType);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_UnsupportedDraft_ReturnsDiagnosticAndDraftDetail()
    {
        using var doc = JsonDocument.Parse("""{"$schema":"http://json-schema.org/draft-07/schema#","type":"string"}""");
        var target = new JavaScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new JavaScriptCodeGenerationOptions()
        };

        var result = await target.GetCapabilitiesAsync(request);

        Assert.False(result.CanGenerate);
        var diagnostic = Assert.Single(result.Diagnostics);
        var detail = Assert.Single(result.UnsupportedDrafts);
        Assert.Equal(CodeGenerationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("javascript.unsupported-draft", diagnostic.Code);
        Assert.Equal("javascript", diagnostic.TargetId);
        Assert.Equal("javascript", detail.TargetId);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_UnsupportedFeature_ReturnsDiagnosticAndFeatureDetail()
    {
        using var doc = JsonDocument.Parse("""{"$schema":"https://json-schema.org/draft/2020-12/schema","$recursiveRef":"#"}""");
        var target = new JavaScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new JavaScriptCodeGenerationOptions()
        };

        var result = await target.GetCapabilitiesAsync(request);

        Assert.False(result.CanGenerate);
        var diagnostic = Assert.Single(result.Diagnostics);
        var detail = Assert.Single(result.UnsupportedFeatures);
        Assert.Equal(CodeGenerationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("javascript.unsupported-feature", diagnostic.Code);
        Assert.Equal("javascript", diagnostic.TargetId);
        Assert.Equal("/$recursiveRef", diagnostic.JsonPointer);
        Assert.Equal("$recursiveRef", detail.FeatureName);
        Assert.Equal("/$recursiveRef", detail.JsonPointer);
        Assert.Equal("javascript", detail.TargetId);
    }

    [Fact]
    public async Task GenerateAsync_EmitsPrimarySourceAndRuntimeArtifacts()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new JavaScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new JavaScriptCodeGenerationOptions
            {
                OutputHints = new CodeGenerationOutputHints
                {
                    BaseFileName = "generated-validator"
                }
            }
        };

        var result = await target.GenerateAsync(request);

        Assert.True(result.Success);
        Assert.Equal(2, result.Artifacts.Count);
        var source = Assert.Single(result.Artifacts, artifact => artifact.Role == GeneratedArtifactRole.Primary);
        var runtime = Assert.Single(result.Artifacts, artifact => artifact.Kind == GeneratedArtifactKind.Runtime);
        Assert.Equal("generated-validator.js", source.RelativePath);
        Assert.Equal(GeneratedArtifactKind.Source, source.Kind);
        Assert.Contains("export function validate(data)", source.Content);
        Assert.Equal(JsRuntime.FileName, runtime.RelativePath);
        Assert.Contains("export class EvaluatedState", runtime.Content);
    }

    [Fact]
    public async Task GenerateAsync_EmitSupportArtifactsFalse_SkipsRuntimeArtifact()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new JavaScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new JavaScriptCodeGenerationOptions
            {
                EmitSupportArtifacts = false
            }
        };

        var result = await target.GenerateAsync(request);

        Assert.True(result.Success);
        var artifact = Assert.Single(result.Artifacts);
        Assert.Equal("validator.js", artifact.RelativePath);
        Assert.Equal(GeneratedArtifactRole.Primary, artifact.Role);
    }

    [Fact]
    public async Task GenerateAsync_OutputHintWithPathSegments_SanitizesPrimaryArtifactName()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new JavaScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new JavaScriptCodeGenerationOptions
            {
                EmitSupportArtifacts = false,
                OutputHints = new CodeGenerationOutputHints
                {
                    BaseFileName = "../nested/bad:name.ts"
                }
            }
        };

        var result = await target.GenerateAsync(request);

        Assert.True(result.Success);
        var artifact = Assert.Single(result.Artifacts);
        Assert.Equal("bad_name.js", artifact.RelativePath);
    }

    [Fact]
    public async Task GenerateAsync_InvalidSchema_ReturnsGenerationDiagnostic()
    {
        using var doc = JsonDocument.Parse("42");
        var target = new JavaScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new JavaScriptCodeGenerationOptions()
        };

        var result = await target.GenerateAsync(request);

        Assert.False(result.Success);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CodeGenerationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("javascript.generation-failed", diagnostic.Code);
        Assert.Equal("javascript", diagnostic.TargetId);
    }

    [Fact]
    public async Task GenerateAsync_WrongOptionsType_ThrowsClearArgumentException()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new JavaScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new CodeGenerationOptions()
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await target.GenerateAsync(request));

        Assert.Contains(typeof(JavaScriptCodeGenerationOptions).FullName!, exception.Message);
        Assert.Contains(typeof(CodeGenerationOptions).FullName!, exception.Message);
        Assert.Equal("request", exception.ParamName);
    }
}
