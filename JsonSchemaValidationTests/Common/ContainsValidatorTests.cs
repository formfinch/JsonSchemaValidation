// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation;

namespace FormFinch.JsonSchemaValidation.Tests.Common;

/// <summary>
/// Tests for contains, minContains, and maxContains validators.
/// </summary>
public class ContainsValidatorTests
{
    #region Basic Contains

    [Fact]
    public void Contains_AtLeastOneMatch_Passes()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "string" }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, "hello", 4]""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """["only strings"]""").Valid);
    }

    [Fact]
    public void Contains_NoMatch_Fails()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "string" }
        }
        """;

        Assert.False(JsonSchemaValidator.Validate(schema, """[1, 2, 3, 4]""").Valid);
    }

    [Fact]
    public void Contains_EmptyArray_Fails()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "string" }
        }
        """;

        Assert.False(JsonSchemaValidator.Validate(schema, "[]").Valid);
    }

    [Fact]
    public void Contains_NonArrayType_Passes()
    {
        var schema = """{ "contains": { "type": "string" } }""";

        // Contains only applies to arrays
        Assert.True(JsonSchemaValidator.Validate(schema, "\"hello\"").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "123").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "{}").Valid);
    }

    #endregion

    #region MinContains

    [Fact]
    public void MinContains_MeetsMinimum_Passes()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "number" },
            "minContains": 2
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, "a"]""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, 3]""").Valid);
    }

    [Fact]
    public void MinContains_BelowMinimum_Fails()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "number" },
            "minContains": 2
        }
        """;

        Assert.False(JsonSchemaValidator.Validate(schema, """[1, "a", "b"]""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """["a", "b", "c"]""").Valid);
    }

    [Fact]
    public void MinContains_Zero_AllowsNoMatches()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "string" },
            "minContains": 0
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, 3]""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "[]").Valid);
    }

    #endregion

    #region MaxContains

    [Fact]
    public void MaxContains_AtOrBelowMax_Passes()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "number" },
            "maxContains": 2
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """[1, "a", "b"]""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, "a"]""").Valid);
    }

    [Fact]
    public void MaxContains_ExceedsMax_Fails()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "number" },
            "maxContains": 2
        }
        """;

        Assert.False(JsonSchemaValidator.Validate(schema, """[1, 2, 3]""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """[1, 2, 3, 4, 5]""").Valid);
    }

    #endregion

    #region Combined MinContains and MaxContains

    [Fact]
    public void MinMaxContains_InRange_Passes()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "integer" },
            "minContains": 2,
            "maxContains": 4
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, "a"]""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, 3, "a"]""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, 3, 4]""").Valid);
    }

    [Fact]
    public void MinMaxContains_OutOfRange_Fails()
    {
        var schema = """
        {
            "type": "array",
            "contains": { "type": "integer" },
            "minContains": 2,
            "maxContains": 4
        }
        """;

        Assert.False(JsonSchemaValidator.Validate(schema, """[1, "a", "b"]""").Valid); // only 1 integer
        Assert.False(JsonSchemaValidator.Validate(schema, """[1, 2, 3, 4, 5]""").Valid); // 5 integers
    }

    #endregion

    #region Complex Contains Schema

    [Fact]
    public void Contains_ComplexSchema_Works()
    {
        var schema = """
        {
            "type": "array",
            "contains": {
                "type": "object",
                "required": ["name"],
                "properties": {
                    "name": { "type": "string" }
                }
            }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """[{"name": "Alice"}, {"other": 1}]""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """[{"other": 1}, {"another": 2}]""").Valid);
    }

    [Fact]
    public void Contains_BooleanSchema_Works()
    {
        var schema = """
        {
            "type": "array",
            "contains": true
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """[1]""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, """["a", 2, null]""").Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "[]").Valid);
    }

    [Fact]
    public void Contains_FalseSchema_NeverMatches()
    {
        var schema = """
        {
            "type": "array",
            "contains": false,
            "minContains": 0
        }
        """;

        // With minContains: 0, it's ok to have no matches
        Assert.True(JsonSchemaValidator.Validate(schema, """[1, 2, 3]""").Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "[]").Valid);
    }

    #endregion

    #region Draft-Specific Behavior

    [Fact]
    public void Draft6_Contains_Works()
    {
        var options = new SchemaValidationOptions { EnableDraft6 = true };
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-06/schema#",
            "type": "array",
            "contains": { "type": "number" }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """[1, "a"]""", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """["a", "b"]""", options).Valid);
    }

    [Fact]
    public void Draft7_Contains_Works()
    {
        var options = new SchemaValidationOptions { EnableDraft7 = true };
        var schema = """
        {
            "$schema": "http://json-schema.org/draft-07/schema#",
            "type": "array",
            "contains": { "const": "special" }
        }
        """;

        Assert.True(JsonSchemaValidator.Validate(schema, """["a", "special", "b"]""", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, """["a", "b", "c"]""", options).Valid);
    }

    #endregion
}
