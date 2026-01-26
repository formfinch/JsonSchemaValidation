// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Draft6;

/// <summary>
/// Tests for Draft 6 format validators (extends Draft 4 with uri-reference and json-pointer).
/// </summary>
public class FormatValidatorTests
{
    private readonly SchemaValidationOptions _options = new()
    {
        Draft6 = new Draft6Options { FormatAssertionEnabled = true }
    };

    #region URI-Reference Format (New in Draft 6)

    [Theory]
    [InlineData("http://example.com", true)] // absolute URI
    [InlineData("https://example.com/path", true)]
    [InlineData("/relative/path", true)] // relative reference
    [InlineData("relative/path", true)] // relative path
    [InlineData("#fragment", true)] // fragment only
    [InlineData("?query=value", true)] // query only
    [InlineData("urn:isbn:0451450523", true)]
    public void UriReference_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-06/schema#", "format": "uri-reference"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region JSON Pointer Format (New in Draft 6)

    [Theory]
    [InlineData("", true)] // empty pointer (root)
    [InlineData("/", true)] // single empty segment
    [InlineData("/foo", true)]
    [InlineData("/foo/bar", true)]
    [InlineData("/foo/0", true)] // array index
    [InlineData("/~0", true)] // escaped ~
    [InlineData("/~1", true)] // escaped /
    [InlineData("/foo/~1bar", true)] // escaped / in segment
    [InlineData("/a~0b", true)] // tilde escape
    [InlineData("/a~1b", true)] // slash escape
    [InlineData("foo", false)] // missing leading /
    [InlineData("/foo/~2", false)] // invalid escape
    public void JsonPointer_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-06/schema#", "format": "json-pointer"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Draft 4 Formats Still Work in Draft 6

    [Theory]
    [InlineData("2023-12-25T10:30:45Z", true)]
    [InlineData("not-a-date", false)]
    public void DateTime_Validation_InDraft6(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-06/schema#", "format": "date-time"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("not-an-email", false)]
    public void Email_Validation_InDraft6(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-06/schema#", "format": "email"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("256.1.1.1", false)]
    public void IPv4_Validation_InDraft6(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-06/schema#", "format": "ipv4"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region FormatValidators Static API

    [Fact]
    public void FormatValidators_StaticMethods_ReturnCorrectResults()
    {
        using var validPointer = JsonDocument.Parse("\"/foo/bar\"");
        using var invalidPointer = JsonDocument.Parse("\"foo\"");
        using var validUriRef = JsonDocument.Parse("\"/relative/path\"");

        Assert.True(JsonSchemaValidation.Draft6.Keywords.Format.FormatValidators.IsValidJsonPointer(validPointer.RootElement));
        Assert.False(JsonSchemaValidation.Draft6.Keywords.Format.FormatValidators.IsValidJsonPointer(invalidPointer.RootElement));
        Assert.True(JsonSchemaValidation.Draft6.Keywords.Format.FormatValidators.IsValidUriReference(validUriRef.RootElement));
    }

    #endregion
}
