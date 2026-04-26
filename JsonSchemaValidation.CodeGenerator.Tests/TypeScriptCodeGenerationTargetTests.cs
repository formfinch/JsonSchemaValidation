// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public sealed class TypeScriptCodeGenerationTargetTests
{
    [Fact]
    public async Task GetCapabilitiesAsync_SupportedSchema_ReturnsDraftSelection()
    {
        using var doc = JsonDocument.Parse("""{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"string"}""");
        var target = new TypeScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new TypeScriptCodeGenerationOptions()
        };

        var result = await target.GetCapabilitiesAsync(request);

        Assert.True(result.CanGenerate);
        Assert.NotNull(result.DraftSelection);
        Assert.Equal("typescript", target.Descriptor.Id);
        Assert.Equal(typeof(TypeScriptCodeGenerationOptions), target.Descriptor.OptionsType);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_UnsupportedDraft_ReturnsDiagnostic()
    {
        using var doc = JsonDocument.Parse("""{"$schema":"http://json-schema.org/draft-07/schema#","type":"string"}""");
        var target = new TypeScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new TypeScriptCodeGenerationOptions()
        };

        var result = await target.GetCapabilitiesAsync(request);

        Assert.False(result.CanGenerate);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CodeGenerationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("typescript.unsupported-draft", diagnostic.Code);
        Assert.Equal("typescript", diagnostic.TargetId);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_UnsupportedFeature_ReturnsDiagnostic()
    {
        using var doc = JsonDocument.Parse("""{"$schema":"https://json-schema.org/draft/2020-12/schema","$recursiveRef":"#"}""");
        var target = new TypeScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new TypeScriptCodeGenerationOptions()
        };

        var result = await target.GetCapabilitiesAsync(request);

        Assert.False(result.CanGenerate);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CodeGenerationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("typescript.unsupported-feature", diagnostic.Code);
        Assert.Equal("typescript", diagnostic.TargetId);
    }

    [Fact]
    public async Task GenerateAsync_EmitsPrimarySourceAndRuntimeArtifacts()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new TypeScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new TypeScriptCodeGenerationOptions
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
        Assert.Equal("generated-validator.ts", source.RelativePath);
        Assert.Equal(GeneratedArtifactKind.Source, source.Kind);
        Assert.Contains("export function validate(data: JsonValue): boolean", source.Content);
        Assert.Equal(TsRuntime.FileName, runtime.RelativePath);
        Assert.StartsWith("// @ts-nocheck", runtime.Content);
    }

    [Fact]
    public async Task GenerateAsync_EmitSupportArtifactsFalse_SkipsRuntimeArtifact()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new TypeScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new TypeScriptCodeGenerationOptions
            {
                EmitSupportArtifacts = false
            }
        };

        var result = await target.GenerateAsync(request);

        Assert.True(result.Success);
        var artifact = Assert.Single(result.Artifacts);
        Assert.Equal("validator.ts", artifact.RelativePath);
        Assert.Equal(GeneratedArtifactRole.Primary, artifact.Role);
    }

    [Fact]
    public async Task GenerateAsync_InvalidSchema_ReturnsGenerationDiagnostic()
    {
        using var doc = JsonDocument.Parse("42");
        var target = new TypeScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new TypeScriptCodeGenerationOptions()
        };

        var result = await target.GenerateAsync(request);

        Assert.False(result.Success);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CodeGenerationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("typescript.generation-failed", diagnostic.Code);
        Assert.Equal("typescript", diagnostic.TargetId);
    }

    [Fact]
    public async Task GenerateAsync_WrongOptionsType_ThrowsClearArgumentException()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new TypeScriptCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new CodeGenerationOptions()
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await target.GenerateAsync(request));

        Assert.Contains(typeof(TypeScriptCodeGenerationOptions).FullName!, exception.Message);
        Assert.Contains(typeof(CodeGenerationOptions).FullName!, exception.Message);
        Assert.Equal("request", exception.ParamName);
    }
}
