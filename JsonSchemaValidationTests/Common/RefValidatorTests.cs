// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Common;

/// <summary>
/// Tests for $ref validator across drafts.
/// </summary>
public class RefValidatorTests
{
    #region Local References

    [Fact]
    public void Ref_LocalDefinition_ResolvesCorrectly()
    {
        var schema = """
        {
            "$defs": {
                "positiveInteger": {
                    "type": "integer",
                    "minimum": 1
                }
            },
            "type": "object",
            "properties": {
                "count": { "$ref": "#/$defs/positiveInteger" }
            }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"count": 5}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"count": 0}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"count": "five"}""").Valid);
    }

    [Fact]
    public void Ref_RecursiveDefinition_Works()
    {
        var schema = """
        {
            "$defs": {
                "node": {
                    "type": "object",
                    "properties": {
                        "value": { "type": "integer" },
                        "children": {
                            "type": "array",
                            "items": { "$ref": "#/$defs/node" }
                        }
                    }
                }
            },
            "$ref": "#/$defs/node"
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"value": 1}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"value": 1, "children": [{"value": 2}]}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"value": 1, "children": [{"value": 2, "children": [{"value": 3}]}]}""").Valid);
    }

    [Fact]
    public void Ref_ToRoot_Works()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" },
                "child": { "$ref": "#" }
            }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "parent"}""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "parent", "child": {"name": "child"}}""").Valid);
    }

    #endregion

    #region Anchors

    [Fact]
    public void Ref_ToAnchor_ResolvesCorrectly()
    {
        var schema = """
        {
            "$defs": {
                "stringType": {
                    "$anchor": "myString",
                    "type": "string"
                }
            },
            "properties": {
                "name": { "$ref": "#myString" }
            }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """{"name": "hello"}""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """{"name": 123}""").Valid);
    }

    #endregion

    #region Ref with Siblings

    [Fact]
    public void Ref_WithSiblings_SiblingsApplied()
    {
        // In Draft 2020-12, $ref can have siblings
        var schema = """
        {
            "$defs": {
                "base": { "type": "integer" }
            },
            "allOf": [
                { "$ref": "#/$defs/base" },
                { "minimum": 0 }
            ]
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, "5").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "-5").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "\"hello\"").Valid);
    }

    #endregion

    #region Draft-Specific Behavior

    [Fact]
    public void Draft4_Ref_IgnoresSiblings()
    {
        var options = new SchemaValidationOptions { EnableDraft4 = true };
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-04/schema#",
            "definitions": {
                "base": { "type": "integer" }
            },
            "$ref": "#/definitions/base",
            "minimum": 10
        }
        """;

        // In Draft 4, $ref overrides siblings, so minimum is ignored
        Assert.True(JsonSchemaValidator.Validate(schema, "5", options).Valid);
    }

    [Fact]
    public void Draft6_Definitions_Works()
    {
        var options = new SchemaValidationOptions { EnableDraft6 = true };
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-06/schema#",
            "definitions": {
                "name": { "type": "string" }
            },
            "$ref": "#/definitions/name"
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, "\"hello\"", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "123", options).Valid);
    }

    [Fact]
    public void Draft7_Ref_Works()
    {
        var options = new SchemaValidationOptions { EnableDraft7 = true };
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-07/schema#",
            "definitions": {
                "positiveInt": { "type": "integer", "minimum": 1 }
            },
            "$ref": "#/definitions/positiveInt"
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, "5", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "0", options).Valid);
    }

    #endregion
}
