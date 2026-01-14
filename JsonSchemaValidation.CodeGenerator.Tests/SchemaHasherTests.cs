using System.Text.Json;
using JsonSchemaValidation.CodeGenerator.CodeGenerator;
using Xunit;

namespace JsonSchemaValidation.CodeGenerator.Tests;

public class SchemaHasherTests
{
    private readonly SchemaHasher _hasher = new();

    [Fact]
    public void ComputeHash_SameSchema_ReturnsSameHash()
    {
        // Arrange
        var schema1 = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var schema2 = JsonDocument.Parse("""{"type": "string"}""").RootElement;

        // Act
        var hash1 = _hasher.ComputeHash(schema1);
        var hash2 = _hasher.ComputeHash(schema2);

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
        var hash1 = _hasher.ComputeHash(schema1);
        var hash2 = _hasher.ComputeHash(schema2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IgnoresMetadataKeywords()
    {
        // Arrange - title and description are metadata, should be ignored
        var schema1 = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var schema2 = JsonDocument.Parse("""{"type": "string", "title": "Test", "description": "A test"}""").RootElement;

        // Act
        var hash1 = _hasher.ComputeHash(schema1);
        var hash2 = _hasher.ComputeHash(schema2);

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
        var hash1 = _hasher.ComputeHash(schema1);
        var hash2 = _hasher.ComputeHash(schema2);

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
        var hash1 = _hasher.ComputeHash(schema1);
        var hash2 = _hasher.ComputeHash(schema2);

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
        var hashTrue = _hasher.ComputeHash(schemaTrue);
        var hashFalse = _hasher.ComputeHash(schemaFalse);

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
        var hash = _hasher.ComputeHash(schema);

        // Assert
        Assert.NotNull(hash);
        Assert.Matches("^[a-f0-9]+$", hash);
    }
}
