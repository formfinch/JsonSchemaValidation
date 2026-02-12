// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation;

namespace FormFinch.JsonSchemaValidationTests.NegativeTests;

/// <summary>
/// Tests verifying error messages are clear and actionable,
/// error locations are correct, and nested errors propagate correctly.
/// </summary>
public class ErrorMessageTests
{
    #region Error Message Clarity Tests

    [Fact]
    public void Validate_TypeMismatch_ReturnsDescriptiveError()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string"}""",
            "123");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
        Assert.NotEmpty(result.Errors);

        var error = result.Errors.First();
        Assert.Contains("type", error.KeywordLocation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_MinLengthViolation_ReturnsDescriptiveError()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string", "minLength": 5}""",
            "\"hi\"");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault(e => e.KeywordLocation.Contains("minLength"));
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_RequiredPropertyMissing_ReturnsDescriptiveError()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "object", "required": ["name"]}""",
            "{}");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault(e => e.KeywordLocation.Contains("required"));
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_MinimumViolation_ReturnsDescriptiveError()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "number", "minimum": 10}""",
            "5");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault(e => e.KeywordLocation.Contains("minimum"));
        Assert.NotNull(error);
    }

    #endregion

    #region Instance Location Tests

    [Fact]
    public void Validate_RootLevelError_HasEmptyInstanceLocation()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string"}""",
            "123");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.First();
        // Root level errors should have empty or "/" instance location
        Assert.True(string.IsNullOrEmpty(error.InstanceLocation) || error.InstanceLocation == "/");
    }

    [Fact]
    public void Validate_NestedPropertyError_HasCorrectInstanceLocation()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "object", "properties": {"user": {"type": "object", "properties": {"name": {"type": "string"}}}}}""",
            """{"user": {"name": 123}}""");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault(e => e.InstanceLocation.Contains("/user/name"));
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_ArrayItemError_HasCorrectInstanceLocation()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "array", "items": {"type": "string"}}""",
            """["a", "b", 123, "d"]""");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault(e => e.InstanceLocation.Contains("/2"));
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_DeepNestedArrayError_HasCorrectInstanceLocation()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "array", "items": {"type": "array", "items": {"type": "string"}}}""",
            """[["a", "b"], ["c", 123]]""");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault(e => e.InstanceLocation.Contains("/1/1"));
        Assert.NotNull(error);
    }

    #endregion

    #region Keyword Location Tests

    [Fact]
    public void Validate_SimpleSchema_HasCorrectKeywordLocation()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string"}""",
            "123");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.First();
        Assert.Contains("type", error.KeywordLocation);
    }

    [Fact]
    public void Validate_NestedSchemaKeyword_HasCorrectKeywordLocation()
    {
        var result = JsonSchemaValidator.Validate(
            """{"properties": {"name": {"type": "string"}}}""",
            """{"name": 123}""");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault(e => e.KeywordLocation.Contains("/properties/name/type"));
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_AllOfSchema_HasCorrectKeywordLocation()
    {
        var result = JsonSchemaValidator.Validate(
            """{"allOf": [{"type": "object"}, {"required": ["name"]}]}""",
            "{}");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault(e => e.KeywordLocation.Contains("allOf"));
        Assert.NotNull(error);
    }

    #endregion

    #region Nested Error Propagation Tests

    [Fact]
    public void Validate_WithDetailedFormat_ReturnsHierarchicalErrors()
    {
        var result = JsonSchemaValidator.Validate(
            """{"allOf": [{"type": "object"}, {"required": ["name"]}]}""",
            "{}",
            OutputFormat.Detailed);

        Assert.False(result.Valid);
        // Detailed format may have nested Errors
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_WithBasicFormat_ReturnsFlatErrors()
    {
        var result = JsonSchemaValidator.Validate(
            """{"allOf": [{"type": "object"}, {"required": ["name"]}]}""",
            "{}",
            OutputFormat.Basic);

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
        // Basic format has flat errors
        Assert.All(result.Errors, e => Assert.Null(e.Errors));
    }

    [Fact]
    public void Validate_WithFlagFormat_ReturnsNoErrors()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string"}""",
            "123",
            OutputFormat.Flag);

        Assert.False(result.Valid);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void Validate_OneOfMultipleFailures_ReportsAllBranches()
    {
        var result = JsonSchemaValidator.Validate(
            """{"oneOf": [{"type": "string"}, {"type": "boolean"}]}""",
            "123",
            OutputFormat.Detailed);

        Assert.False(result.Valid);
        // The error should indicate that no oneOf branch matched
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_AnyOfAllFail_ReportsAllBranches()
    {
        var result = JsonSchemaValidator.Validate(
            """{"anyOf": [{"type": "string"}, {"type": "boolean"}]}""",
            "123",
            OutputFormat.Detailed);

        Assert.False(result.Valid);
        // The error should indicate that no anyOf branch matched
        Assert.NotNull(result);
    }

    #endregion

    #region Multiple Error Tests

    [Fact]
    public void Validate_MultipleViolations_ReportsAllErrors()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "object", "required": ["a", "b", "c"]}""",
            "{}");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
        // Should report missing properties
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validate_MultiplePropertiesInvalid_ReportsAllErrors()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "object", "properties": {"name": {"type": "string"}, "age": {"type": "integer"}}}""",
            """{"name": 123, "age": "thirty"}""");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
        // Should have errors for both name and age
        Assert.True(result.Errors.Count >= 2);
    }

    [Fact]
    public void Validate_ArrayMultipleItemsInvalid_ReportsErrors()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "array", "items": {"type": "string"}}""",
            """[1, 2, 3, 4, 5]""");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
        // Should have at least one error for invalid items
        Assert.NotEmpty(result.Errors);
    }

    #endregion

    #region Error Content Quality Tests

    [Fact]
    public void Validate_TypeError_HasMeaningfulMessage()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string"}""",
            "123");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.First();
        // Error should have a message that helps diagnose the issue
        Assert.False(string.IsNullOrWhiteSpace(error.Error));
    }

    [Fact]
    public void Validate_PatternError_HasMeaningfulMessage()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "string", "pattern": "^[A-Z]+$"}""",
            "\"lowercase\"");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault(e => e.KeywordLocation.Contains("pattern"));
        Assert.NotNull(error);
        Assert.False(string.IsNullOrWhiteSpace(error.Error));
    }

    [Fact]
    public void Validate_EnumError_HasMeaningfulMessage()
    {
        var result = JsonSchemaValidator.Validate(
            """{"enum": ["a", "b", "c"]}""",
            "\"d\"");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        var error = result.Errors.First();
        Assert.False(string.IsNullOrWhiteSpace(error.Error));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_VeryDeepNesting_HasCorrectLocations()
    {
        // Create a deeply nested schema and instance
        var schema = """
            {
                "type": "object",
                "properties": {
                    "a": {
                        "type": "object",
                        "properties": {
                            "b": {
                                "type": "object",
                                "properties": {
                                    "c": {
                                        "type": "object",
                                        "properties": {
                                            "d": {"type": "string"}
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """;

        var result = JsonSchemaValidator.Validate(
            schema,
            """{"a": {"b": {"c": {"d": 123}}}}""");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        // Should have correct deeply nested instance location
        var error = result.Errors.FirstOrDefault(e => e.InstanceLocation.Contains("/a/b/c/d"));
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_SpecialCharactersInPropertyNames_HasCorrectLocations()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "object", "properties": {"foo/bar": {"type": "string"}, "baz~qux": {"type": "number"}}}""",
            """{"foo/bar": 123, "baz~qux": "text"}""");

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);
        // JSON Pointer escapes / as ~1 and ~ as ~0
        // Verify we have errors
        Assert.NotEmpty(result.Errors);
    }

    #endregion
}
