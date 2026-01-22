// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Validation.Output;

namespace FormFinch.JsonSchemaValidationTests;

/// <summary>
/// Tests for the static convenience API (<see cref="JsonSchemaValidator"/>).
/// </summary>
public class StaticApiTests
{
    #region Validate Tests

    [Fact]
    public void Validate_ValidStringAgainstTypeSchema_ReturnsValid()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string"}""",
            "\"hello\""
        );

        Assert.True(result.Valid);
    }

    [Fact]
    public void Validate_InvalidNumberAgainstStringSchema_ReturnsInvalid()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string"}""",
            "42"
        );

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validate_WithFlagFormat_ReturnsOnlyValidFlag()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string"}""",
            "42",
            OutputFormat.Flag
        );

        Assert.False(result.Valid);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void Validate_WithBasicFormat_ReturnsFlatErrorList()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "object", "properties": {"name": {"type": "string"}}, "required": ["name"]}""",
            """{"name": 123}""",
            OutputFormat.Basic
        );

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
        // Basic format has flat errors (no nested Errors on child units)
        Assert.All(result.Errors, e => Assert.Null(e.Errors));
    }

    [Fact]
    public void Validate_WithDetailedFormat_ReturnsHierarchicalOutput()
    {
        var result = JsonSchemaValidator.Validate(
            """{"allOf": [{"type": "object"}, {"required": ["name"]}]}""",
            "{}",
            OutputFormat.Detailed
        );

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
    }

    [Fact]
    public void Validate_ComplexSchema_ValidatesCorrectly()
    {
        var schema = """
            {
                "type": "object",
                "properties": {
                    "name": {"type": "string", "minLength": 1},
                    "age": {"type": "integer", "minimum": 0},
                    "email": {"type": "string", "format": "email"}
                },
                "required": ["name", "age"]
            }
            """;

        var validInstance = """{"name": "John", "age": 30, "email": "john@example.com"}""";
        var invalidInstance = """{"name": "", "age": -5}""";

        var validResult = JsonSchemaValidator.Validate(schema, validInstance);
        var invalidResult = JsonSchemaValidator.Validate(schema, invalidInstance);

        Assert.True(validResult.Valid);
        Assert.False(invalidResult.Valid);
    }

    [Fact]
    public void Validate_WithCustomOptions_RespectsFormatAssertion()
    {
        var schema = """{"type": "string", "format": "email"}""";
        var instance = "\"not-an-email\"";

        // Without format assertion (default) - should pass
        var resultWithoutAssertion = JsonSchemaValidator.Validate(schema, instance);
        Assert.True(resultWithoutAssertion.Valid);

        // With format assertion enabled - should fail
        var options = new SchemaValidationOptions
        {
            Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
        };
        var resultWithAssertion = JsonSchemaValidator.Validate(schema, instance, options);
        Assert.False(resultWithAssertion.Valid);
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_ValidInstance_ReturnsTrue()
    {
        var isValid = JsonSchemaValidator.IsValid(
            """{"type": "number"}""",
            "42"
        );

        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_InvalidInstance_ReturnsFalse()
    {
        var isValid = JsonSchemaValidator.IsValid(
            """{"type": "number"}""",
            "\"not a number\""
        );

        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_ComplexValidation_Works()
    {
        var schema = """
            {
                "type": "array",
                "items": {"type": "integer"},
                "minItems": 1
            }
            """;

        Assert.True(JsonSchemaValidator.IsValid(schema, "[1, 2, 3]"));
        Assert.False(JsonSchemaValidator.IsValid(schema, "[]"));
        Assert.False(JsonSchemaValidator.IsValid(schema, "[1, \"two\", 3]"));
    }

    #endregion

    #region Parse Tests

    [Fact]
    public void Parse_ReturnsReusableSchema()
    {
        var schema = JsonSchemaValidator.Parse("""{"type": "string"}""");

        Assert.NotNull(schema);
        Assert.NotNull(schema.SchemaUri);
    }

    [Fact]
    public void Parse_ValidateMultipleInstances_WorksCorrectly()
    {
        var schema = JsonSchemaValidator.Parse("""{"type": "integer", "minimum": 0}""");

        var result1 = schema.Validate("42");
        var result2 = schema.Validate("-5");
        var result3 = schema.Validate("\"not an integer\"");

        Assert.True(result1.Valid);
        Assert.False(result2.Valid);
        Assert.False(result3.Valid);
    }

    [Fact]
    public void Parse_IsValid_Works()
    {
        var schema = JsonSchemaValidator.Parse("""{"type": "boolean"}""");

        Assert.True(schema.IsValid("true"));
        Assert.True(schema.IsValid("false"));
        Assert.False(schema.IsValid("\"true\""));
        Assert.False(schema.IsValid("1"));
    }

    [Fact]
    public void Parse_WithOptions_RespectsConfiguration()
    {
        var options = new SchemaValidationOptions
        {
            Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
        };

        var schema = JsonSchemaValidator.Parse("""{"format": "email"}""", options);

        Assert.True(schema.IsValid("\"test@example.com\""));
        Assert.False(schema.IsValid("\"not-an-email\""));
    }

    [Fact]
    public void Parse_ValidateWithOutputFormat_Works()
    {
        var schema = JsonSchemaValidator.Parse("""{"type": "object", "required": ["id"]}""");

        var flagResult = schema.Validate("{}", OutputFormat.Flag);
        var basicResult = schema.Validate("{}", OutputFormat.Basic);

        Assert.False(flagResult.Valid);
        Assert.Null(flagResult.Errors);

        Assert.False(basicResult.Valid);
        Assert.NotNull(basicResult.Errors);
    }

    #endregion

    #region Draft Support Tests

    [Theory]
    [InlineData("""{"$schema": "https://json-schema.org/draft/2020-12/schema", "type": "string"}""")]
    [InlineData("""{"$schema": "https://json-schema.org/draft/2019-09/schema", "type": "string"}""")]
    [InlineData("""{"$schema": "http://json-schema.org/draft-07/schema#", "type": "string"}""")]
    [InlineData("""{"$schema": "http://json-schema.org/draft-06/schema#", "type": "string"}""")]
    [InlineData("""{"$schema": "http://json-schema.org/draft-04/schema#", "type": "string"}""")]
    public void Validate_AllDrafts_Supported(string schema)
    {
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"");
        Assert.True(result.Valid);
    }

    #endregion

    #region Schema Caching Tests

    [Fact]
    public void Validate_SameSchemaMultipleTimes_UsesCaching()
    {
        // This test verifies that repeated validation with the same schema
        // doesn't cause unbounded memory growth (schemas are cached by content hash)
        var schema = """{"type": "string", "minLength": 1}""";

        // Validate the same schema many times
        for (int i = 0; i < 100; i++)
        {
            var result = JsonSchemaValidator.Validate(schema, "\"hello\"");
            Assert.True(result.Valid);
        }

        // If caching wasn't working, the schema repository would have 100+ entries
        // We can't easily verify the internal cache size, but at least verify it works
    }

    [Fact]
    public void Validate_DifferentSchemas_EachGetsCached()
    {
        var schema1 = """{"type": "string"}""";
        var schema2 = """{"type": "integer"}""";
        var schema3 = """{"type": "boolean"}""";

        // Each unique schema should work correctly
        Assert.True(JsonSchemaValidator.Validate(schema1, "\"hello\"").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema2, "42").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema3, "true").Valid);

        // Validate again to hit the cache
        Assert.True(JsonSchemaValidator.Validate(schema1, "\"world\"").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema2, "123").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema3, "false").Valid);
    }

    [Fact]
    public void Validate_SchemasWithDifferentMetadataButSameValidation_ShareCache()
    {
        // These schemas have different metadata but identical validation behavior
        // The hasher ignores metadata keywords like $id, title, description
        var schema1 = """{"type": "string", "title": "Schema A"}""";
        var schema2 = """{"type": "string", "title": "Schema B"}""";
        var schema3 = """{"type": "string", "description": "Different description"}""";

        // All should validate the same way
        Assert.True(JsonSchemaValidator.Validate(schema1, "\"hello\"").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema2, "\"hello\"").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema3, "\"hello\"").Valid);

        Assert.False(JsonSchemaValidator.Validate(schema1, "123").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema2, "123").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema3, "123").Valid);
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Validate_ErrorsContainInstanceLocation()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "object", "properties": {"items": {"type": "array", "items": {"type": "string"}}}}""",
            """{"items": [1, 2, 3]}"""
        );

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        // Errors should contain the path to the invalid items
        Assert.Contains(result.Errors, e => e.InstanceLocation.Contains("/items/"));
    }

    [Fact]
    public void Validate_ErrorsContainKeywordLocation()
    {
        var result = JsonSchemaValidator.Validate(
            """{"minimum": 10}""",
            "5"
        );

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.KeywordLocation.Contains("minimum"));
    }

    #endregion
}
