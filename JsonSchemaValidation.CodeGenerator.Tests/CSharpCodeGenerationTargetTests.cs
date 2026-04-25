// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public sealed class CSharpCodeGenerationTargetTests
{
    [Fact]
    public async Task GetCapabilitiesAsync_SupportedSchema_ReturnsDraftSelection()
    {
        using var doc = JsonDocument.Parse("""{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"string"}""");
        var target = new CSharpCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new CSharpCodeGenerationOptions()
        };

        var result = await target.GetCapabilitiesAsync(request);

        Assert.True(result.CanGenerate);
        Assert.NotNull(result.DraftSelection);
        Assert.Equal("csharp", target.Descriptor.Id);
        Assert.Equal(typeof(CSharpCodeGenerationOptions), target.Descriptor.OptionsType);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_UnsupportedDraft_ReturnsDiagnostic()
    {
        using var doc = JsonDocument.Parse("""{"$schema":"https://example.com/unknown-schema","type":"string"}""");
        var target = new CSharpCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new CSharpCodeGenerationOptions()
        };

        var result = await target.GetCapabilitiesAsync(request);

        Assert.False(result.CanGenerate);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CodeGenerationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("csharp.unsupported-draft", diagnostic.Code);
        Assert.Equal("csharp", diagnostic.TargetId);
    }

    [Fact]
    public async Task GenerateAsync_EmitsPrimaryCSharpSourceArtifact()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new CSharpCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new CSharpCodeGenerationOptions
            {
                OutputHints = new CodeGenerationOutputHints
                {
                    NamespaceName = "Generated.Tests",
                    TypeName = "GeneratedValidator"
                }
            }
        };

        var result = await target.GenerateAsync(request);

        Assert.True(result.Success);
        var artifact = Assert.Single(result.Artifacts);
        Assert.Equal("GeneratedValidator.cs", artifact.RelativePath);
        Assert.Equal(GeneratedArtifactKind.Source, artifact.Kind);
        Assert.Equal(GeneratedArtifactRole.Primary, artifact.Role);
        Assert.Contains("namespace Generated.Tests", artifact.Content);
        Assert.Contains("class GeneratedValidator", artifact.Content);
    }

    [Fact]
    public async Task GenerateAsync_InvalidSchema_ReturnsGenerationDiagnostic()
    {
        using var doc = JsonDocument.Parse("42");
        var target = new CSharpCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new CSharpCodeGenerationOptions()
        };

        var result = await target.GenerateAsync(request);

        Assert.False(result.Success);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CodeGenerationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("csharp.generation-failed", diagnostic.Code);
        Assert.Equal("csharp", diagnostic.TargetId);
    }

    [Fact]
    public async Task GenerateAsync_WrongOptionsType_ThrowsClearArgumentException()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new CSharpCodeGenerationTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new CodeGenerationOptions()
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await target.GenerateAsync(request));

        Assert.Contains(typeof(CSharpCodeGenerationOptions).FullName!, exception.Message);
        Assert.Contains(typeof(CodeGenerationOptions).FullName!, exception.Message);
        Assert.Equal("request", exception.ParamName);
    }
}
