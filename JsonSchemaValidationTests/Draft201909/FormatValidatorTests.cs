// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Draft201909;

/// <summary>
/// Tests for Draft 2019-09 format validators.
/// </summary>
public class FormatValidatorTests
{
    private readonly SchemaValidationOptions _options = new()
    {
        Draft201909 = new Draft201909Options { FormatAssertionEnabled = true }
    };

    #region Duration Format (New in 2019-09)

    [Theory]
    [InlineData("P1Y", true)] // 1 year
    [InlineData("P1M", true)] // 1 month
    [InlineData("P1D", true)] // 1 day
    [InlineData("PT1H", true)] // 1 hour
    [InlineData("PT1M", true)] // 1 minute
    [InlineData("PT1S", true)] // 1 second
    [InlineData("P1Y2M3DT4H5M6S", true)] // full duration
    [InlineData("P1W", true)] // 1 week
    [InlineData("PT0S", true)] // zero seconds
    [InlineData("P0D", true)] // zero days
    [InlineData("1Y", false)] // missing P
    [InlineData("P", false)] // no duration specified
    [InlineData("PT", false)] // no time components
    [InlineData("not-a-duration", false)]
    [InlineData("", false)]
    public void Duration_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "format": "duration"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region UUID Format

    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", true)]
    [InlineData("550E8400-E29B-41D4-A716-446655440000", true)]
    [InlineData("00000000-0000-0000-0000-000000000000", true)]
    [InlineData("550e8400e29b41d4a716446655440000", false)]
    [InlineData("not-a-uuid", false)]
    public void Uuid_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "format": "uuid"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Date Format

    [Theory]
    [InlineData("2023-12-25", true)]
    [InlineData("2024-02-29", true)] // leap year
    [InlineData("2023-02-29", false)] // not a leap year
    [InlineData("2023-13-25", false)] // month 13
    [InlineData("not-a-date", false)]
    public void Date_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "format": "date"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Time Format

    [Theory]
    [InlineData("10:30:45Z", true)]
    [InlineData("10:30:45+05:30", true)]
    [InlineData("23:59:59Z", true)]
    [InlineData("23:59:60Z", true)] // leap second
    [InlineData("10:30:45", false)] // missing timezone
    [InlineData("not-a-time", false)]
    public void Time_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "format": "time"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region URI Template Format (New in 2019-09)

    [Theory]
    [InlineData("http://example.com/users/{id}", true)]
    [InlineData("http://example.com/{+path}", true)]
    [InlineData("/users/{userId}/posts/{postId}", true)]
    [InlineData("http://example.com", true)]
    public void UriTemplate_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "format": "uri-template"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region IRI Format

    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("https://example.com/path", true)]
    public void Iri_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "format": "iri"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region IRI-Reference Format

    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("/relative/path", true)]
    [InlineData("#fragment", true)]
    public void IriReference_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "format": "iri-reference"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Relative JSON Pointer Format

    [Theory]
    [InlineData("0", true)] // current value
    [InlineData("1/foo", true)] // parent's foo
    [InlineData("2/bar/0", true)] // grandparent's bar[0]
    [InlineData("0#", true)] // current key
    [InlineData("1#", true)] // parent key
    [InlineData("/foo", false)] // regular JSON pointer, not relative
    [InlineData("foo", false)] // no leading number
    public void RelativeJsonPointer_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "format": "relative-json-pointer"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Regex Format

    [Theory]
    [InlineData("^[a-z]+$", true)]
    [InlineData("[0-9]+", true)]
    [InlineData(".*", true)]
    [InlineData("[", false)] // unclosed bracket
    [InlineData("(unclosed", false)]
    public void Regex_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "format": "regex"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region FormatValidators Static API

    [Fact]
    public void FormatValidators_Duration_StaticMethod()
    {
        using var validDuration = JsonDocument.Parse("\"P1Y2M3D\"");
        using var invalidDuration = JsonDocument.Parse("\"not-a-duration\"");

        Assert.True(JsonSchemaValidation.Draft201909.Keywords.Format.FormatValidators.IsValidDuration(validDuration.RootElement));
        Assert.False(JsonSchemaValidation.Draft201909.Keywords.Format.FormatValidators.IsValidDuration(invalidDuration.RootElement));
    }

    [Fact]
    public void FormatValidators_Uuid_StaticMethod()
    {
        using var validUuid = JsonDocument.Parse("\"550e8400-e29b-41d4-a716-446655440000\"");
        using var invalidUuid = JsonDocument.Parse("\"not-a-uuid\"");

        Assert.True(JsonSchemaValidation.Draft201909.Keywords.Format.FormatValidators.IsValidUuid(validUuid.RootElement));
        Assert.False(JsonSchemaValidation.Draft201909.Keywords.Format.FormatValidators.IsValidUuid(invalidUuid.RootElement));
    }

    [Fact]
    public void FormatValidators_UriTemplate_StaticMethod()
    {
        using var valid = JsonDocument.Parse("\"http://example.com/{id}\"");
        using var alsoValid = JsonDocument.Parse("\"http://example.com\"");

        Assert.True(JsonSchemaValidation.Draft201909.Keywords.Format.FormatValidators.IsValidUriTemplate(valid.RootElement));
        Assert.True(JsonSchemaValidation.Draft201909.Keywords.Format.FormatValidators.IsValidUriTemplate(alsoValid.RootElement));
    }

    [Fact]
    public void FormatValidators_RelativeJsonPointer_StaticMethod()
    {
        using var valid = JsonDocument.Parse("\"0/foo\"");
        using var invalid = JsonDocument.Parse("\"/foo\"");

        Assert.True(JsonSchemaValidation.Draft201909.Keywords.Format.FormatValidators.IsValidRelativeJsonPointer(valid.RootElement));
        Assert.False(JsonSchemaValidation.Draft201909.Keywords.Format.FormatValidators.IsValidRelativeJsonPointer(invalid.RootElement));
    }

    #endregion
}
