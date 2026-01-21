// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public class SubschemaExtractorTests
{
    private readonly SubschemaExtractor _extractor = new();

    [Fact]
    public void ExtractUniqueSubschemas_SimpleSchema_ReturnsOneSchema()
    {
        // Arrange
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;

        // Act
        var result = _extractor.ExtractUniqueSubschemas(schema);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void ExtractUniqueSubschemas_SchemaWithProperties_ExtractsPropertySchemas()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "age": { "type": "integer" }
                }
            }
            """).RootElement;

        // Act
        var result = _extractor.ExtractUniqueSubschemas(schema);

        // Assert - root + string schema + integer schema
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ExtractUniqueSubschemas_DuplicateSchemas_DeduplicatesByHash()
    {
        // Arrange - both name and title use the same schema {"type": "string"}
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "title": { "type": "string" }
                }
            }
            """).RootElement;

        // Act
        var result = _extractor.ExtractUniqueSubschemas(schema);

        // Assert - root + ONE string schema (deduplicated)
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ExtractUniqueSubschemas_NestedSchemas_ExtractsAll()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "address": {
                        "type": "object",
                        "properties": {
                            "street": { "type": "string" }
                        }
                    }
                }
            }
            """).RootElement;

        // Act
        var result = _extractor.ExtractUniqueSubschemas(schema);

        // Assert - root + address schema + street schema (string)
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ExtractUniqueSubschemas_BooleanSchema_Handled()
    {
        // Arrange
        var schema = JsonDocument.Parse("true").RootElement;

        // Act
        var result = _extractor.ExtractUniqueSubschemas(schema);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void ExtractUniqueSubschemas_AllOfSchema_ExtractsSubschemas()
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
        var result = _extractor.ExtractUniqueSubschemas(schema);

        // Assert - root + 2 subschemas
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ExtractUniqueSubschemas_ItemsSchema_ExtractsItemSchema()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "array",
                "items": { "type": "string" }
            }
            """).RootElement;

        // Act
        var result = _extractor.ExtractUniqueSubschemas(schema);

        // Assert - root + items schema
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ExtractUniqueSubschemas_IfThenElse_ExtractsAll()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "if": { "type": "string" },
                "then": { "minLength": 1 },
                "else": { "type": "number" }
            }
            """).RootElement;

        // Act
        var result = _extractor.ExtractUniqueSubschemas(schema);

        // Assert - root + if + then + else
        Assert.Equal(4, result.Count);
    }
}
