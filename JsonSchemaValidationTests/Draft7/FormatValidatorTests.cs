// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Draft7;

/// <summary>
/// Tests for Draft 7 format validators.
/// </summary>
public class FormatValidatorTests
{
    private readonly SchemaValidationOptions _options = new()
    {
        Draft7 = new Draft7Options { FormatAssertionEnabled = true }
    };

    #region Date Format (New in Draft 7)

    [Theory]
    [InlineData("2023-12-25", true)]
    [InlineData("2024-02-29", true)] // leap year
    [InlineData("2023-02-29", false)] // not a leap year
    [InlineData("2023-13-25", false)] // month 13
    [InlineData("2023-12-32", false)] // day 32
    [InlineData("2023-00-15", false)] // month 0
    [InlineData("2023-12-00", false)] // day 0
    [InlineData("not-a-date", false)]
    [InlineData("2023/12/25", false)] // wrong separator
    [InlineData("12-25-2023", false)] // wrong order
    public void Date_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "format": "date"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Time Format (New in Draft 7)

    [Theory]
    [InlineData("10:30:45Z", true)]
    [InlineData("10:30:45+05:30", true)]
    [InlineData("10:30:45-05:00", true)]
    [InlineData("10:30:45.123Z", true)] // with fractional seconds
    [InlineData("23:59:59Z", true)]
    [InlineData("23:59:60Z", true)] // leap second
    [InlineData("00:00:00Z", true)]
    [InlineData("24:00:00Z", false)] // hour 24 invalid
    [InlineData("10:60:45Z", false)] // minute 60 invalid
    [InlineData("10:30:61Z", false)] // second 61 invalid
    [InlineData("10:30:45", false)] // missing timezone
    [InlineData("not-a-time", false)]
    public void Time_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "format": "time"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Regex Format (New in Draft 7)

    [Theory]
    [InlineData("^[a-z]+$", true)]
    [InlineData("[0-9]+", true)]
    [InlineData(".*", true)]
    [InlineData("a|b", true)]
    [InlineData("(foo|bar)", true)]
    [InlineData("[", false)] // unclosed bracket
    [InlineData("(unclosed", false)] // unclosed paren
    public void Regex_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "format": "regex"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region UUID Format

    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", true)] // lowercase
    [InlineData("550E8400-E29B-41D4-A716-446655440000", true)] // uppercase
    [InlineData("00000000-0000-0000-0000-000000000000", true)] // nil UUID
    [InlineData("550e8400e29b41d4a716446655440000", false)] // missing hyphens
    [InlineData("not-a-uuid", false)]
    [InlineData("", false)]
    public void Uuid_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "format": "uuid"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region IDN-Email Format (New in Draft 7)

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("not-an-email", false)]
    public void IdnEmail_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "format": "idn-email"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region IDN-Hostname Format (New in Draft 7)

    [Theory]
    [InlineData("example.com", true)]
    [InlineData("-invalid.com", false)]
    public void IdnHostname_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "format": "idn-hostname"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Draft 6 Formats Still Work in Draft 7

    [Theory]
    [InlineData("/foo/bar", true)]
    [InlineData("foo", false)]
    public void JsonPointer_Validation_InDraft7(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "format": "json-pointer"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Content Keywords (Annotation-Only in Draft 7)

    [Theory]
    [InlineData("base64")]
    [InlineData("quoted-printable")]
    public void ContentEncoding_AlwaysPassesValidation(string encoding)
    {
        var schema = $"{{\"$schema\": \"http://json-schema.org/draft-07/schema#\", \"contentEncoding\": \"{encoding}\"}}";

        // Content encoding is annotation-only, always passes
        Assert.True(JsonSchemaValidator.Validate(schema, "\"any value\"", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "\"!!!invalid base64!!!\"", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "123", _options).Valid);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/plain")]
    [InlineData("image/png")]
    public void ContentMediaType_AlwaysPassesValidation(string mediaType)
    {
        var schema = $"{{\"$schema\": \"http://json-schema.org/draft-07/schema#\", \"contentMediaType\": \"{mediaType}\"}}";

        // Content media type is annotation-only, always passes
        Assert.True(JsonSchemaValidator.Validate(schema, "\"any value\"", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "\"not valid json\"", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "123", _options).Valid);
    }

    #endregion

    #region FormatValidators Static API

    [Fact]
    public void FormatValidators_Date_StaticMethod()
    {
        using var validDate = JsonDocument.Parse("\"2023-12-25\"");
        using var invalidDate = JsonDocument.Parse("\"not-a-date\"");

        Assert.True(JsonSchemaValidation.Draft7.Keywords.Format.FormatValidators.IsValidDate(validDate.RootElement));
        Assert.False(JsonSchemaValidation.Draft7.Keywords.Format.FormatValidators.IsValidDate(invalidDate.RootElement));
    }

    [Fact]
    public void FormatValidators_Time_StaticMethod()
    {
        using var validTime = JsonDocument.Parse("\"10:30:45Z\"");
        using var invalidTime = JsonDocument.Parse("\"not-a-time\"");

        Assert.True(JsonSchemaValidation.Draft7.Keywords.Format.FormatValidators.IsValidTime(validTime.RootElement));
        Assert.False(JsonSchemaValidation.Draft7.Keywords.Format.FormatValidators.IsValidTime(invalidTime.RootElement));
    }

    [Fact]
    public void FormatValidators_Regex_StaticMethod()
    {
        using var validRegex = JsonDocument.Parse("\"^[a-z]+$\"");
        using var invalidRegex = JsonDocument.Parse("\"[\"");

        Assert.True(JsonSchemaValidation.Draft7.Keywords.Format.FormatValidators.IsValidRegex(validRegex.RootElement));
        Assert.False(JsonSchemaValidation.Draft7.Keywords.Format.FormatValidators.IsValidRegex(invalidRegex.RootElement));
    }

    #endregion
}
