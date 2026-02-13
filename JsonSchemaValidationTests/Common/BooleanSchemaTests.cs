// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation;

namespace FormFinch.JsonSchemaValidation.Tests.Common;

/// <summary>
/// Tests for boolean schemas (true/false as schema values).
/// Boolean schemas were introduced in Draft 6 - they are not valid in Draft 3 or Draft 4.
/// </summary>
public class BooleanSchemaTests
{
    #region Draft 2020-12

    [Theory]
    [InlineData("true", "\"hello\"", true)]
    [InlineData("true", "123", true)]
    [InlineData("true", "null", true)]
    [InlineData("true", "{}", true)]
    [InlineData("true", "[]", true)]
    [InlineData("false", "\"hello\"", false)]
    [InlineData("false", "123", false)]
    [InlineData("false", "null", false)]
    [InlineData("false", "{}", false)]
    [InlineData("false", "[]", false)]
    public void Draft202012_BooleanSchema_ValidatesCorrectly(string schema, string instance, bool expectedValid)
    {
        var result = JsonSchemaValidator.Validate(schema, instance);

        Assert.Equal(expectedValid, result.Valid);
    }

    [Fact]
    public void Draft202012_TrueSchema_InProperties_AllowsAnyValue()
    {
        var schema = """{"type": "object", "properties": {"name": true}}""";

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "test"}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": 123}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": null}""").Valid);
    }

    [Fact]
    public void Draft202012_FalseSchema_InProperties_RejectsAnyValue()
    {
        var schema = """{"type": "object", "properties": {"forbidden": false}}""";

        Assert.False(JsonSchemaValidator.Validate(schema, """{"forbidden": "test"}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"forbidden": null}""").Valid);
    }

    [Fact]
    public void Draft202012_FalseSchema_InAdditionalProperties_RejectsExtra()
    {
        var schema = """{"type": "object", "properties": {"name": {"type": "string"}}, "additionalProperties": false}""";

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "test"}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": "test", "extra": 1}""").Valid);
    }

    [Fact]
    public void Draft202012_TrueSchema_InAdditionalProperties_AllowsExtra()
    {
        var schema = """{"type": "object", "properties": {"name": {"type": "string"}}, "additionalProperties": true}""";

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "test", "anything": "goes", "count": 42}""").Valid);
    }

    #endregion

    #region Draft 2019-09

    [Theory]
    [InlineData("true", "\"hello\"", true)]
    [InlineData("true", "123", true)]
    [InlineData("false", "\"hello\"", false)]
    [InlineData("false", "123", false)]
    public void Draft201909_BooleanSchema_ValidatesCorrectly(string schema, string instance, bool expectedValid)
    {
        // Use $id to trigger draft detection (2019-09 uses $id not id)
        var fullSchema = schema == "true" || schema == "false"
            ? schema
            : $$"""{"$schema": "https://json-schema.org/draft/2019-09/schema", {{schema.TrimStart('{').TrimEnd('}')}} }""";

        var options = new SchemaValidationOptions { EnableDraft201909 = true };
        var result = JsonSchemaValidator.Validate(fullSchema, instance, options);

        Assert.Equal(expectedValid, result.Valid);
    }

    [Fact]
    public void Draft201909_FalseSchema_InAdditionalProperties_RejectsExtra()
    {
        var schema = """
        {
            "$schema": "https://json-schema.org/draft/2019-09/schema",
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "additionalProperties": false
        }
        """;
        var options = new SchemaValidationOptions { EnableDraft201909 = true };

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "test"}""", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": "test", "extra": 1}""", options).Valid);
    }

    #endregion

    #region Draft 7

    [Theory]
    [InlineData("true", "\"hello\"", true)]
    [InlineData("true", "123", true)]
    [InlineData("false", "\"hello\"", false)]
    [InlineData("false", "123", false)]
    public void Draft7_BooleanSchema_ValidatesCorrectly(string schema, string instance, bool expectedValid)
    {
        var options = new SchemaValidationOptions { EnableDraft7 = true };
        var result = JsonSchemaValidator.Validate(schema, instance, options);

        Assert.Equal(expectedValid, result.Valid);
    }

    [Fact]
    public void Draft7_FalseSchema_InAdditionalProperties_RejectsExtra()
    {
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-07/schema#",
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "additionalProperties": false
        }
        """;
        var options = new SchemaValidationOptions { EnableDraft7 = true };

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "test"}""", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": "test", "extra": 1}""", options).Valid);
    }

    #endregion

    #region Draft 6

    [Theory]
    [InlineData("true", "\"hello\"", true)]
    [InlineData("true", "123", true)]
    [InlineData("false", "\"hello\"", false)]
    [InlineData("false", "123", false)]
    public void Draft6_BooleanSchema_ValidatesCorrectly(string schema, string instance, bool expectedValid)
    {
        var options = new SchemaValidationOptions { EnableDraft6 = true };
        var result = JsonSchemaValidator.Validate(schema, instance, options);

        Assert.Equal(expectedValid, result.Valid);
    }

    [Fact]
    public void Draft6_FalseSchema_InAdditionalProperties_RejectsExtra()
    {
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-06/schema#",
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "additionalProperties": false
        }
        """;
        var options = new SchemaValidationOptions { EnableDraft6 = true };

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "test"}""", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": "test", "extra": 1}""", options).Valid);
    }

    #endregion

    #region Draft 4 and Draft 3 - Boolean schemas not supported

    // Note: Boolean schemas (true/false as root schema) are NOT valid in Draft 4 or Draft 3.
    // These drafts require schemas to be objects. The additionalProperties: false pattern
    // still works because that's a boolean value within an object schema, not a boolean schema.

    [Fact]
    public void Draft4_AdditionalPropertiesFalse_RejectsExtra()
    {
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-04/schema#",
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "additionalProperties": false
        }
        """;
        var options = new SchemaValidationOptions { EnableDraft4 = true };

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "test"}""", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": "test", "extra": 1}""", options).Valid);
    }

    [Fact]
    public void Draft3_AdditionalPropertiesFalse_RejectsExtra()
    {
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-03/schema#",
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "additionalProperties": false
        }
        """;
        var options = new SchemaValidationOptions { EnableDraft3 = true };

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "test"}""", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": "test", "extra": 1}""", options).Valid);
    }

    #endregion
}
