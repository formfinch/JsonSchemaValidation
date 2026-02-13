// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation;

namespace FormFinch.JsonSchemaValidation.Tests.Common;

/// <summary>
/// Tests for if/then/else conditional validators.
/// </summary>
public class IfThenElseValidatorTests
{
    #region Basic If/Then/Else

    [Fact]
    public void IfThenElse_IfMatches_AppliesThen()
    {
        var schema = """
        {
            "if": { "properties": { "type": { "const": "number" } } },
            "then": { "properties": { "value": { "type": "number" } } },
            "else": { "properties": { "value": { "type": "string" } } }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"type": "number", "value": 42}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"type": "number", "value": "text"}""").Valid);
    }

    [Fact]
    public void IfThenElse_IfDoesNotMatch_AppliesElse()
    {
        var schema = """
        {
            "if": { "properties": { "type": { "const": "number" } } },
            "then": { "properties": { "value": { "type": "number" } } },
            "else": { "properties": { "value": { "type": "string" } } }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"type": "string", "value": "text"}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"type": "string", "value": 42}""").Valid);
    }

    #endregion

    #region If/Then Only

    [Fact]
    public void IfThen_IfMatches_AppliesThen()
    {
        var schema = """
        {
            "if": { "properties": { "enabled": { "const": true } } },
            "then": { "required": ["config"] }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"enabled": true, "config": {}}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"enabled": true}""").Valid);
    }

    [Fact]
    public void IfThen_IfDoesNotMatch_PassesWithoutThen()
    {
        var schema = """
        {
            "if": { "properties": { "enabled": { "const": true } }, "required": ["enabled"] },
            "then": { "required": ["config"] }
        }
        """;

        // enabled=false doesn't match if (const: true fails), so then is not applied
        Assert.True(JsonSchemaValidator.Validate(schema, """{"enabled": false}""").Valid);
        // no enabled property doesn't match if (required fails), so then is not applied
        Assert.True(JsonSchemaValidator.Validate(schema, """{}""").Valid);
    }

    #endregion

    #region If/Else Only

    [Fact]
    public void IfElse_IfDoesNotMatch_AppliesElse()
    {
        var schema = """
        {
            "if": { "properties": { "premium": { "const": true } } },
            "else": { "properties": { "features": { "maxItems": 3 } } }
        }
        """;

        // Non-premium users limited to 3 features
        Assert.True(JsonSchemaValidator.Validate(schema, """{"premium": false, "features": [1, 2, 3]}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"premium": false, "features": [1, 2, 3, 4]}""").Valid);
    }

    [Fact]
    public void IfElse_IfMatches_ElseNotApplied()
    {
        var schema = """
        {
            "if": { "properties": { "premium": { "const": true } } },
            "else": { "properties": { "features": { "maxItems": 3 } } }
        }
        """;

        // Premium users have no feature limit (else not applied)
        Assert.True(JsonSchemaValidator.Validate(schema, """{"premium": true, "features": [1, 2, 3, 4, 5]}""").Valid);
    }

    #endregion

    #region Nested If/Then/Else

    [Fact]
    public void NestedIfThenElse_WorksCorrectly()
    {
        var schema = """
        {
            "if": { "properties": { "type": { "const": "A" } } },
            "then": {
                "if": { "properties": { "subtype": { "const": "1" } } },
                "then": { "required": ["fieldA1"] },
                "else": { "required": ["fieldA2"] }
            },
            "else": { "required": ["fieldB"] }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"type": "A", "subtype": "1", "fieldA1": true}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"type": "A", "subtype": "2", "fieldA2": true}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"type": "B", "fieldB": true}""").Valid);
    }

    #endregion

    #region Boolean If Schemas

    [Fact]
    public void IfTrue_AlwaysAppliesThen()
    {
        var schema = """
        {
            "if": true,
            "then": { "type": "string" },
            "else": { "type": "number" }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, "\"hello\"").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "123").Valid);
    }

    [Fact]
    public void IfFalse_AlwaysAppliesElse()
    {
        var schema = """
        {
            "if": false,
            "then": { "type": "string" },
            "else": { "type": "number" }
        }
        """;

        Assert.False(JsonSchemaValidator.Validate(schema, "\"hello\"").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "123").Valid);
    }

    #endregion

    #region Draft-Specific Behavior

    [Fact]
    public void Draft7_IfThenElse_Works()
    {
        var options = new SchemaValidationOptions { EnableDraft7 = true };
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-07/schema#",
            "if": { "type": "string" },
            "then": { "minLength": 1 },
            "else": { "minimum": 0 }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, "\"hello\"", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "\"\"", options).Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "5", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "-5", options).Valid);
    }

    [Fact]
    public void Draft201909_IfThenElse_Works()
    {
        var options = new SchemaValidationOptions { EnableDraft201909 = true };
        var schema = """
        {
            "$schema": "https://json-schema.org/draft/2019-09/schema",
            "if": { "type": "array" },
            "then": { "minItems": 1 },
            "else": true
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, "[1]", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "[]", options).Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "\"not an array\"", options).Valid);
    }

    #endregion

    #region Combined with Other Keywords

    [Fact]
    public void IfThenElse_CombinedWithAllOf()
    {
        var schema = """
        {
            "allOf": [
                { "type": "object" },
                {
                    "if": { "properties": { "kind": { "const": "person" } } },
                    "then": { "required": ["name"] },
                    "else": { "required": ["title"] }
                }
            ]
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"kind": "person", "name": "Alice"}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"kind": "company", "title": "Acme Inc"}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"kind": "person"}""").Valid);
    }

    #endregion
}
