// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Common;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public class SchemaHasherTests
{

    [Fact]
    public void ComputeHash_SameSchema_ReturnsSameHash()
    {
        // Arrange
        var schema1 = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var schema2 = JsonDocument.Parse("""{"type": "string"}""").RootElement;

        // Act
        var hash1 = SchemaHasher.ComputeHash(schema1);
        var hash2 = SchemaHasher.ComputeHash(schema2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentSchema_ReturnsDifferentHash()
    {
        // Arrange
        var schema1 = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var schema2 = JsonDocument.Parse("""{"type": "number"}""").RootElement;

        // Act
        var hash1 = SchemaHasher.ComputeHash(schema1);
        var hash2 = SchemaHasher.ComputeHash(schema2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_AnnotationKeywordsAffectHash()
    {
        // Arrange - annotation keywords are part of the schema's observable behavior
        var schema1 = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var schema2 = JsonDocument.Parse("""{"type": "string", "title": "Test", "description": "A test"}""").RootElement;

        // Act
        var hash1 = SchemaHasher.ComputeHash(schema1);
        var hash2 = SchemaHasher.ComputeHash(schema2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IgnoresNonBehavioralKeywords()
    {
        // Arrange - $id and $comment do not affect validation behavior
        var schema1 = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var schema2 = JsonDocument.Parse("""{"type": "string", "$id": "urn:test:schema", "$comment": "for docs only"}""").RootElement;

        // Act
        var hash1 = SchemaHasher.ComputeHash(schema1);
        var hash2 = SchemaHasher.ComputeHash(schema2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_PropertyOrderDoesNotMatter()
    {
        // Arrange
        var schema1 = JsonDocument.Parse("""{"type": "string", "minLength": 1}""").RootElement;
        var schema2 = JsonDocument.Parse("""{"minLength": 1, "type": "string"}""").RootElement;

        // Act
        var hash1 = SchemaHasher.ComputeHash(schema1);
        var hash2 = SchemaHasher.ComputeHash(schema2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_BooleanSchema_ReturnsConsistentHash()
    {
        // Arrange
        var schema1 = JsonDocument.Parse("true").RootElement;
        var schema2 = JsonDocument.Parse("true").RootElement;

        // Act
        var hash1 = SchemaHasher.ComputeHash(schema1);
        var hash2 = SchemaHasher.ComputeHash(schema2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_TrueVsFalseSchema_ReturnsDifferentHash()
    {
        // Arrange
        var schemaTrue = JsonDocument.Parse("true").RootElement;
        var schemaFalse = JsonDocument.Parse("false").RootElement;

        // Act
        var hashTrue = SchemaHasher.ComputeHash(schemaTrue);
        var hashFalse = SchemaHasher.ComputeHash(schemaFalse);

        // Assert
        Assert.NotEqual(hashTrue, hashFalse);
    }

    [Fact]
    public void ComputeHash_ComplexSchema_ReturnsValidHash()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "age": { "type": "integer", "minimum": 0 }
                },
                "required": ["name"]
            }
            """).RootElement;

        // Act
        var hash = SchemaHasher.ComputeHash(schema);

        // Assert
        Assert.NotNull(hash);
        Assert.Matches("^[a-f0-9]+$", hash);
    }
}
