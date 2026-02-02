// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public class SchemaCodeGeneratorTests
{
    private readonly SchemaCodeGenerator _generator = new();

    [Fact]
    public void Generate_SimpleTypeSchema_ReturnsSuccessResult()
    {
        // Arrange
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;

        // Act
        var result = _generator.Generate(schema, "Test.Namespace", "TestValidator");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GeneratedCode);
        Assert.NotNull(result.FileName);
    }

    [Fact]
    public void Generate_BooleanTrueSchema_ReturnsSuccessResult()
    {
        // Arrange
        var schema = JsonDocument.Parse("true").RootElement;

        // Act
        var result = _generator.Generate(schema, "Test.Namespace", "TestValidator");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("return true;", result.GeneratedCode);
    }

    [Fact]
    public void Generate_BooleanFalseSchema_ReturnsSuccessResult()
    {
        // Arrange
        var schema = JsonDocument.Parse("false").RootElement;

        // Act
        var result = _generator.Generate(schema, "Test.Namespace", "TestValidator");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("return false;", result.GeneratedCode);
    }

    [Fact]
    public void Generate_IncludesNamespace()
    {
        // Arrange
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;

        // Act
        var result = _generator.Generate(schema, "My.Custom.Namespace", "TestValidator");

        // Assert
        Assert.Contains("namespace My.Custom.Namespace", result.GeneratedCode);
    }

    [Fact]
    public void Generate_IncludesClassName()
    {
        // Arrange
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "MyCustomValidator");

        // Assert
        Assert.Contains("public sealed class MyCustomValidator", result.GeneratedCode);
    }

    [Fact]
    public void Generate_ImplementsICompiledValidator()
    {
        // Arrange
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "TestValidator");

        // Assert
        Assert.Contains(": ICompiledValidator", result.GeneratedCode);
        Assert.Contains("using FormFinch.JsonSchemaValidation.Abstractions;", result.GeneratedCode);
    }

    [Fact]
    public void Generate_IncludesIsValidMethod()
    {
        // Arrange
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "TestValidator");

        // Assert
        Assert.Contains("public bool IsValid(JsonElement instance)", result.GeneratedCode);
    }

    [Fact]
    public void Generate_WithSchemaId_IncludesSchemaUri()
    {
        // Arrange
        var schema = JsonDocument.Parse("""{"$id": "https://example.com/test", "type": "string"}""").RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "TestValidator");

        // Assert
        Assert.Contains("public Uri SchemaUri", result.GeneratedCode);
        Assert.Contains("https://example.com/test", result.GeneratedCode);
    }

    [Fact]
    public void Generate_RequiredKeyword_GeneratesPropertyChecks()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "required": ["name", "email"]
            }
            """).RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "TestValidator");

        // Assert
        Assert.Contains("TryGetProperty(\"name\"", result.GeneratedCode);
        Assert.Contains("TryGetProperty(\"email\"", result.GeneratedCode);
    }

    [Fact]
    public void Generate_EnumKeyword_GeneratesDeepEqualsChecks()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "enum": ["red", "green", "blue"]
            }
            """).RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "TestValidator");

        // Assert
        Assert.Contains("DeepEquals(", result.GeneratedCode);
    }

    [Fact]
    public void Generate_PatternKeyword_GeneratesRegexField()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "string",
                "pattern": "^[a-z]+$"
            }
            """).RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "TestValidator");

        // Assert
        Assert.Contains("Regex", result.GeneratedCode);
        Assert.Contains("IsMatch", result.GeneratedCode);
    }

    [Fact]
    public void Generate_AllOfKeyword_GeneratesAllChecks()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "allOf": [
                    { "type": "string" },
                    { "minLength": 1 }
                ]
            }
            """).RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "TestValidator");

        // Assert
        // Should generate calls to validate both subschemas
        Assert.Contains("Validate_", result.GeneratedCode);
    }

    [Fact]
    public void Generate_AnyOfKeyword_GeneratesOrLogic()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "anyOf": [
                    { "type": "string" },
                    { "type": "number" }
                ]
            }
            """).RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "TestValidator");

        // Assert
        Assert.Contains("_anyValid_", result.GeneratedCode);
    }

    [Fact]
    public void Generate_ComplexSchema_Succeeds()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string", "minLength": 1 },
                    "age": { "type": "integer", "minimum": 0 },
                    "tags": {
                        "type": "array",
                        "items": { "type": "string" },
                        "minItems": 1
                    }
                },
                "required": ["name"],
                "additionalProperties": false
            }
            """).RootElement;

        // Act
        var result = _generator.Generate(schema, "Test", "PersonValidator");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GeneratedCode);
    }
}
