// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Compiler;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Tests.Compiler;

/// <summary>
/// Tests for RuntimeValidatorFactory - the runtime schema compiler.
/// </summary>
public class RuntimeValidatorFactoryTests : IDisposable
{
    private readonly RuntimeValidatorFactory _factory;

    public RuntimeValidatorFactoryTests()
    {
        _factory = new RuntimeValidatorFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Single Schema Compilation

    [Fact]
    public void Compile_SimpleSchema_ReturnsValidator()
    {
        var schema = """{"type": "string"}""";

        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);
    }

    [Fact]
    public void Compile_ComplexSchema_ReturnsValidator()
    {
        var schema = """
            {
                "type": "object",
                "properties": {
                    "name": {"type": "string"},
                    "age": {"type": "integer", "minimum": 0}
                },
                "required": ["name"]
            }
            """;

        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);
    }

    [Fact]
    public void Compile_JsonElement_ReturnsValidator()
    {
        using var doc = JsonDocument.Parse("""{"type": "number"}""");

        var validator = _factory.Compile(doc.RootElement);

        Assert.NotNull(validator);
    }

    [Fact]
    public void Compile_BooleanSchema_ReturnsValidator()
    {
        var trueValidator = _factory.Compile("true");
        var falseValidator = _factory.Compile("false");

        Assert.NotNull(trueValidator);
        Assert.NotNull(falseValidator);
    }

    [Fact]
    public void CompiledValidator_ValidatesCorrectly()
    {
        var schema = """{"type": "string", "minLength": 3}""";

        var validator = _factory.Compile(schema);

        using var validDoc = JsonDocument.Parse("\"hello\"");
        using var invalidDoc = JsonDocument.Parse("\"hi\"");
        using var wrongTypeDoc = JsonDocument.Parse("123");

        Assert.True(validator.IsValid(validDoc.RootElement));
        Assert.False(validator.IsValid(invalidDoc.RootElement));
        Assert.False(validator.IsValid(wrongTypeDoc.RootElement));
    }

    #endregion

    #region Caching

    [Fact]
    public void Compile_SameSchemaTwice_ReturnsCachedValidator()
    {
        var schema = """{"type": "string"}""";

        var validator1 = _factory.Compile(schema);
        var validator2 = _factory.Compile(schema);

        Assert.Same(validator1, validator2);
    }

    [Fact]
    public void Compile_DifferentSchemas_ReturnsDifferentValidators()
    {
        var schema1 = """{"type": "string"}""";
        var schema2 = """{"type": "number"}""";

        var validator1 = _factory.Compile(schema1);
        var validator2 = _factory.Compile(schema2);

        Assert.NotSame(validator1, validator2);
    }

    [Fact]
    public void IsCached_AfterCompile_ReturnsTrue()
    {
        var schema = """{"type": "boolean"}""";

        Assert.False(_factory.IsCached(schema));

        _factory.Compile(schema);

        Assert.True(_factory.IsCached(schema));
    }

    [Fact]
    public void ClearCache_RemovesCachedValidators()
    {
        var schema = """{"type": "array"}""";

        _factory.Compile(schema);
        Assert.True(_factory.IsCached(schema));

        _factory.ClearCache();

        Assert.False(_factory.IsCached(schema));
    }

    #endregion

    #region Batch Compilation

    [Fact]
    public void CompileAll_MultipleSchemas_CompilesAll()
    {
        var schemas = new[]
        {
            """{"type": "string"}""",
            """{"type": "number"}""",
            """{"type": "boolean"}"""
        };

        var validators = _factory.CompileAll(schemas);

        Assert.Equal(3, validators.Count);
        Assert.All(validators.Values, v => Assert.NotNull(v));
    }

    [Fact]
    public void CompileAll_AllUniqueSchemas_CompilesAll()
    {
        var schemas = new[]
        {
            """{"type": "string"}""",
            """{"type": "integer"}""",
            """{"type": "number"}"""
        };

        var validators = _factory.CompileAll(schemas);

        Assert.Equal(3, validators.Count);
        Assert.All(validators.Values, v => Assert.NotNull(v));
    }

    [Fact]
    public void CompileAll_EmptyList_ReturnsEmpty()
    {
        var validators = _factory.CompileAll(Array.Empty<string>());

        Assert.Empty(validators);
    }

    [Fact]
    public void CompileAll_JsonElements_CompilesAll()
    {
        using var doc1 = JsonDocument.Parse("""{"type": "string"}""");
        using var doc2 = JsonDocument.Parse("""{"type": "number"}""");

        var validators = _factory.CompileAll(new[] { doc1.RootElement, doc2.RootElement });

        Assert.Equal(2, validators.Count);
    }

    #endregion

    #region Schema Variations

    [Theory]
    [InlineData("""{"type": "string"}""")]
    [InlineData("""{"type": "number"}""")]
    [InlineData("""{"type": "integer"}""")]
    [InlineData("""{"type": "boolean"}""")]
    [InlineData("""{"type": "null"}""")]
    [InlineData("""{"type": "array"}""")]
    [InlineData("""{"type": "object"}""")]
    public void Compile_AllBasicTypes_Succeeds(string schema)
    {
        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);
    }

    [Fact]
    public void Compile_SchemaWithAllOf_Succeeds()
    {
        var schema = """
            {
                "allOf": [
                    {"type": "object"},
                    {"required": ["name"]}
                ]
            }
            """;

        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);
    }

    [Fact]
    public void Compile_SchemaWithAnyOf_Succeeds()
    {
        var schema = """
            {
                "anyOf": [
                    {"type": "string"},
                    {"type": "number"}
                ]
            }
            """;

        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);
    }

    [Fact]
    public void Compile_SchemaWithOneOf_Succeeds()
    {
        var schema = """
            {
                "oneOf": [
                    {"type": "string"},
                    {"type": "number"}
                ]
            }
            """;

        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);
    }

    [Fact]
    public void Compile_SchemaWithNot_Succeeds()
    {
        var schema = """{"not": {"type": "string"}}""";

        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);
    }

    [Fact]
    public void Compile_SchemaWithIfThenElse_Succeeds()
    {
        var schema = """
            {
                "if": {"type": "string"},
                "then": {"minLength": 1},
                "else": {"type": "number"}
            }
            """;

        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);
    }

    [Fact]
    public void Compile_SchemaWithPattern_Succeeds()
    {
        var schema = """{"type": "string", "pattern": "^[a-z]+$"}""";

        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);

        using var validDoc = JsonDocument.Parse("\"abc\"");
        using var invalidDoc = JsonDocument.Parse("\"ABC\"");

        Assert.True(validator.IsValid(validDoc.RootElement));
        Assert.False(validator.IsValid(invalidDoc.RootElement));
    }

    [Fact]
    public void Compile_SchemaWithFormat_Succeeds()
    {
        var schema = """{"type": "string", "format": "email"}""";

        var validator = _factory.Compile(schema);

        Assert.NotNull(validator);
    }

    #endregion

    #region Numeric Constraints

    [Theory]
    [InlineData("""{"type": "number", "minimum": 0}""", "5", true)]
    [InlineData("""{"type": "number", "minimum": 0}""", "-1", false)]
    [InlineData("""{"type": "number", "maximum": 100}""", "50", true)]
    [InlineData("""{"type": "number", "maximum": 100}""", "150", false)]
    [InlineData("""{"type": "integer", "multipleOf": 5}""", "15", true)]
    [InlineData("""{"type": "integer", "multipleOf": 5}""", "13", false)]
    public void Compile_NumericConstraints_ValidatesCorrectly(string schema, string instance, bool expected)
    {
        var validator = _factory.Compile(schema);

        using var doc = JsonDocument.Parse(instance);

        Assert.Equal(expected, validator.IsValid(doc.RootElement));
    }

    #endregion

    #region String Constraints

    [Theory]
    [InlineData("""{"type": "string", "minLength": 3}""", "\"hello\"", true)]
    [InlineData("""{"type": "string", "minLength": 3}""", "\"hi\"", false)]
    [InlineData("""{"type": "string", "maxLength": 5}""", "\"hi\"", true)]
    [InlineData("""{"type": "string", "maxLength": 5}""", "\"hello world\"", false)]
    public void Compile_StringConstraints_ValidatesCorrectly(string schema, string instance, bool expected)
    {
        var validator = _factory.Compile(schema);

        using var doc = JsonDocument.Parse(instance);

        Assert.Equal(expected, validator.IsValid(doc.RootElement));
    }

    #endregion

    #region Array Constraints

    [Fact]
    public void Compile_ArrayItems_ValidatesCorrectly()
    {
        var schema = """{"type": "array", "items": {"type": "number"}}""";

        var validator = _factory.Compile(schema);

        using var validDoc = JsonDocument.Parse("[1, 2, 3]");
        using var invalidDoc = JsonDocument.Parse("[1, \"two\", 3]");

        Assert.True(validator.IsValid(validDoc.RootElement));
        Assert.False(validator.IsValid(invalidDoc.RootElement));
    }

    [Fact]
    public void Compile_ArrayMinMaxItems_ValidatesCorrectly()
    {
        var schema = """{"type": "array", "minItems": 2, "maxItems": 4}""";

        var validator = _factory.Compile(schema);

        using var tooFew = JsonDocument.Parse("[1]");
        using var justRight = JsonDocument.Parse("[1, 2, 3]");
        using var tooMany = JsonDocument.Parse("[1, 2, 3, 4, 5]");

        Assert.False(validator.IsValid(tooFew.RootElement));
        Assert.True(validator.IsValid(justRight.RootElement));
        Assert.False(validator.IsValid(tooMany.RootElement));
    }

    #endregion

    #region Object Constraints

    [Fact]
    public void Compile_ObjectProperties_ValidatesCorrectly()
    {
        var schema = """
            {
                "type": "object",
                "properties": {
                    "name": {"type": "string"},
                    "age": {"type": "integer"}
                }
            }
            """;

        var validator = _factory.Compile(schema);

        using var validDoc = JsonDocument.Parse("""{"name": "John", "age": 30}""");
        using var invalidDoc = JsonDocument.Parse("""{"name": "John", "age": "thirty"}""");

        Assert.True(validator.IsValid(validDoc.RootElement));
        Assert.False(validator.IsValid(invalidDoc.RootElement));
    }

    [Fact]
    public void Compile_ObjectRequired_ValidatesCorrectly()
    {
        var schema = """
            {
                "type": "object",
                "required": ["name"]
            }
            """;

        var validator = _factory.Compile(schema);

        using var validDoc = JsonDocument.Parse("""{"name": "John"}""");
        using var invalidDoc = JsonDocument.Parse("""{"age": 30}""");

        Assert.True(validator.IsValid(validDoc.RootElement));
        Assert.False(validator.IsValid(invalidDoc.RootElement));
    }

    #endregion

    #region Dispose Pattern

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var factory = new RuntimeValidatorFactory();

        factory.Dispose();
        factory.Dispose(); // should not throw
    }

    #endregion
}
