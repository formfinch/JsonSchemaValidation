// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Common;

/// <summary>
/// Tests for unevaluatedItems and unevaluatedProperties validators.
/// </summary>
public class UnevaluatedValidatorTests
{
    #region UnevaluatedProperties Basic

    [Fact]
    public void UnevaluatedProperties_False_RejectsUnknownProperties()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" }
            },
            "unevaluatedProperties": false
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "Alice"}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": "Alice", "age": 30}""").Valid);
    }

    [Fact]
    public void UnevaluatedProperties_Schema_ValidatesUnknown()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" }
            },
            "unevaluatedProperties": { "type": "number" }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "Alice"}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "Alice", "age": 30}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": "Alice", "city": "NYC"}""").Valid);
    }

    [Fact]
    public void UnevaluatedProperties_True_AllowsAll()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" }
            },
            "unevaluatedProperties": true
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "Alice", "anything": "goes", "count": 42}""").Valid);
    }

    #endregion

    #region UnevaluatedProperties with AllOf

    [Fact]
    public void UnevaluatedProperties_AllOf_PropertiesFromAllOfAreEvaluated()
    {
        var schema = """
        {
            "allOf": [
                {
                    "type": "object",
                    "properties": {
                        "name": { "type": "string" }
                    }
                },
                {
                    "properties": {
                        "age": { "type": "integer" }
                    }
                }
            ],
            "unevaluatedProperties": false
        }
        """;

        // Both name and age are evaluated through allOf
        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "Alice", "age": 30}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": "Alice", "age": 30, "extra": true}""").Valid);
    }

    #endregion

    #region UnevaluatedProperties with IfThenElse

    [Fact]
    public void UnevaluatedProperties_IfThenElse_ThenPropertiesEvaluated()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "type": { "type": "string" }
            },
            "if": { "properties": { "type": { "const": "employee" } } },
            "then": { "properties": { "department": { "type": "string" } } },
            "else": { "properties": { "company": { "type": "string" } } },
            "unevaluatedProperties": false
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"type": "employee", "department": "Engineering"}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"type": "contractor", "company": "Acme"}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"type": "employee", "unknown": true}""").Valid);
    }

    #endregion

    #region UnevaluatedItems Basic

    [Fact]
    public void UnevaluatedItems_False_RejectsExtraItems()
    {
        var schema = """
        {
            "type": "array",
            "prefixItems": [
                { "type": "string" },
                { "type": "number" }
            ],
            "unevaluatedItems": false
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """["hello", 42]""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """["hello"]""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """["hello", 42, "extra"]""").Valid);
    }

    [Fact]
    public void UnevaluatedItems_Schema_ValidatesExtraItems()
    {
        var schema = """
        {
            "type": "array",
            "prefixItems": [
                { "type": "string" }
            ],
            "unevaluatedItems": { "type": "number" }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """["hello", 1, 2, 3]""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """["hello", "another string"]""").Valid);
    }

    [Fact]
    public void UnevaluatedItems_True_AllowsAll()
    {
        var schema = """
        {
            "type": "array",
            "prefixItems": [
                { "type": "string" }
            ],
            "unevaluatedItems": true
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """["hello", 1, true, null, {}]""").Valid);
    }

    #endregion

    #region UnevaluatedItems with Contains

    [Fact]
    public void UnevaluatedItems_Contains_ItemsMatchedByContainsAreEvaluated()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "string" },
            "unevaluatedItems": { "type": "number" }
        }
        """;

        // The string item is evaluated by contains, remaining must be numbers
        Assert.True(JsonSchemaValidator.Validate(schema, """["hello", 1, 2, 3]""").Valid);
    }

    #endregion

    #region UnevaluatedItems with AllOf

    [Fact]
    public void UnevaluatedItems_AllOf_ItemsFromAllOfAreEvaluated()
    {
        var schema = """
        {
            "allOf": [
                {
                    "prefixItems": [{ "type": "string" }]
                },
                {
                    "prefixItems": [true, { "type": "number" }]
                }
            ],
            "unevaluatedItems": false
        }
        """;

        // First 2 items are evaluated through allOf
        Assert.True(JsonSchemaValidator.Validate(schema, """["hello", 42]""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """["hello", 42, "extra"]""").Valid);
    }

    #endregion

    #region UnevaluatedItems with Items

    [Fact]
    public void UnevaluatedItems_WithItems_AllEvaluatedByItems()
    {
        var schema = """
        {
            "type": "array",
            "items": { "type": "integer" },
            "unevaluatedItems": false
        }
        """;

        // All items are evaluated by items keyword
        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, 3, 4, 5]""").Valid);
    }

    #endregion

    #region Draft-Specific Behavior

    [Fact]
    public void Draft201909_UnevaluatedProperties_Works()
    {
        var options = new SchemaValidationOptions { EnableDraft201909 = true };
        var schema = """
        {
            "$schema": "https://json-schema.org/draft/2019-09/schema",
            "type": "object",
            "properties": { "a": true },
            "unevaluatedProperties": false
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"a": 1}""", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"a": 1, "b": 2}""", options).Valid);
    }

    [Fact]
    public void Draft202012_UnevaluatedItems_Works()
    {
        var schema = """
        {
            "type": "array",
            "prefixItems": [{ "type": "string" }],
            "unevaluatedItems": false
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """["hello"]""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """["hello", 42]""").Valid);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void UnevaluatedProperties_EmptyObject_Passes()
    {
        var schema = """
        {
            "type": "object",
            "unevaluatedProperties": false
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, "{}").Valid);
    }

    [Fact]
    public void UnevaluatedItems_EmptyArray_Passes()
    {
        var schema = """
        {
            "type": "array",
            "unevaluatedItems": false
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, "[]").Valid);
    }

    [Fact]
    public void UnevaluatedProperties_NonObject_Passes()
    {
        var schema = """
        {
            "unevaluatedProperties": false
        }
        """;

        // unevaluatedProperties only applies to objects
        Assert.True(JsonSchemaValidator.Validate(schema, "\"string\"").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "123").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "[]").Valid);
    }

    [Fact]
    public void UnevaluatedItems_NonArray_Passes()
    {
        var schema = """
        {
            "unevaluatedItems": false
        }
        """;

        // unevaluatedItems only applies to arrays
        Assert.True(JsonSchemaValidator.Validate(schema, "\"string\"").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "123").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "{}").Valid);
    }

    #endregion
}
