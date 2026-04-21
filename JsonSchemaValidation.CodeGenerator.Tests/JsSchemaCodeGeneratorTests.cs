// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public class JsSchemaCodeGeneratorTests
{
    private readonly JsSchemaCodeGenerator _generator = new();

    [Fact]
    public void Generate_BooleanTrue_EmitsTrueModule()
    {
        var result = _generator.Generate(JsonDocument.Parse("true").RootElement);
        Assert.True(result.Success, result.Error);
        Assert.Contains("return true;", result.GeneratedCode);
        Assert.Contains("export function validate(data)", result.GeneratedCode);
        Assert.Contains("export default", result.GeneratedCode);
    }

    [Fact]
    public void Generate_BooleanFalse_EmitsFalseModule()
    {
        var result = _generator.Generate(JsonDocument.Parse("false").RootElement);
        Assert.True(result.Success, result.Error);
        Assert.Contains("return false;", result.GeneratedCode);
    }

    [Fact]
    public void Generate_SkeletonModule_ExportsValidate()
    {
        // Phase 1: no keyword emitters registered yet, so the body reduces to the default
        // "return true;" skeleton. This confirms the module shell is well-formed.
        var result = _generator.Generate(JsonDocument.Parse("""{ "type": "string" }""").RootElement);
        Assert.True(result.Success, result.Error);
        Assert.Contains("export function validate(data)", result.GeneratedCode);
        Assert.Contains("export const schemaUri", result.GeneratedCode);
    }

    [Fact]
    public void Generate_RejectsUnsupportedFeature_ViaGate()
    {
        var result = _generator.Generate(JsonDocument.Parse("""
            { "type": "object", "unevaluatedProperties": false }
            """).RootElement);
        Assert.False(result.Success);
        Assert.Contains("unevaluatedProperties", result.Error);
    }

    [Fact]
    public void Generate_RejectsExternalRef_ViaGate()
    {
        var result = _generator.Generate(JsonDocument.Parse("""
            { "$ref": "https://example.com/other.json" }
            """).RootElement);
        Assert.False(result.Success);
        Assert.Contains("external $ref", result.Error);
    }

    [Fact]
    public void Generate_SetsSchemaUri_FromId()
    {
        var result = _generator.Generate(JsonDocument.Parse("""
            { "$id": "https://example.com/s.json", "type": "string" }
            """).RootElement);
        Assert.True(result.Success, result.Error);
        Assert.Contains("\"https://example.com/s.json\"", result.GeneratedCode);
    }

    [Fact]
    public void Generate_NoSchemaUri_EmitsNull()
    {
        var result = _generator.Generate(JsonDocument.Parse("""{ "type": "string" }""").RootElement);
        Assert.True(result.Success, result.Error);
        Assert.Contains("export const schemaUri = null;", result.GeneratedCode);
    }
}
