// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public sealed class CodeGenerationTargetTests
{
    [Fact]
    public async Task GenerateAsync_CorrectOptionsType_UsesTypedOptions()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new TestTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new TestOptions { Marker = "expected" }
        };

        var result = await target.GenerateAsync(request);

        Assert.True(result.Success);
        var artifact = Assert.Single(result.Artifacts);
        Assert.Equal("expected.txt", artifact.RelativePath);
    }

    [Fact]
    public void GenerateAsync_MismatchedOptionsType_ThrowsClearArgumentException()
    {
        using var doc = JsonDocument.Parse("""{"type":"string"}""");
        var target = new TestTarget();
        var request = new CodeGenerationRequest
        {
            Schema = doc.RootElement,
            Options = new OtherOptions()
        };

        var ex = Assert.Throws<ArgumentException>(() => target.GenerateAsync(request));

        Assert.Contains(typeof(TestOptions).FullName!, ex.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(OtherOptions).FullName!, ex.Message, StringComparison.Ordinal);
        Assert.Equal("request", ex.ParamName);
    }

    private sealed class TestTarget : CodeGenerationTarget<TestOptions>
    {
        public override CodeGenerationTargetDescriptor Descriptor { get; } = new()
        {
            Id = "test",
            DisplayName = "Test",
            OptionsType = typeof(TestOptions)
        };

        protected override ValueTask<CodeGenerationCapabilityResult> GetCapabilitiesAsync(
            CodeGenerationRequest request,
            TestOptions options,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new CodeGenerationCapabilityResult { CanGenerate = true });
        }

        protected override ValueTask<CodeGenerationResult> GenerateAsync(
            CodeGenerationRequest request,
            TestOptions options,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(CodeGenerationResult.Succeeded(new GeneratedArtifact
            {
                RelativePath = $"{options.Marker}.txt",
                Content = "",
                Kind = GeneratedArtifactKind.Source
            }));
        }
    }

    private sealed class TestOptions : CodeGenerationOptions
    {
        public required string Marker { get; init; }
    }

    private sealed class OtherOptions : CodeGenerationOptions;
}
