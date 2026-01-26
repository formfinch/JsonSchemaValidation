// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Draft4;

/// <summary>
/// Tests for Draft 4 format validators.
/// </summary>
public class FormatValidatorTests
{
    private readonly SchemaValidationOptions _options = new()
    {
        Draft4 = new Draft4Options { FormatAssertionEnabled = true }
    };

    #region DateTime Format

    [Theory]
    [InlineData("2023-12-25T10:30:45Z", true)]
    [InlineData("2023-12-25T10:30:45+05:30", true)]
    [InlineData("2023-12-25T10:30:45-05:00", true)]
    [InlineData("2023-12-25T10:30:45.123Z", true)]
    [InlineData("2023-12-25t10:30:45z", true)] // lowercase t and z
    [InlineData("2023-12-31T23:59:60Z", true)] // leap second
    [InlineData("2023-02-30T10:30:45Z", false)] // Feb 30 invalid
    [InlineData("2023-13-25T10:30:45Z", false)] // month 13 invalid
    [InlineData("2023-12-32T10:30:45Z", false)] // day 32 invalid
    [InlineData("2023-12-25T10:30:61Z", false)] // second 61 invalid
    [InlineData("not-a-date", false)]
    [InlineData("2023-12-25", false)] // date only, no time
    [InlineData("10:30:45Z", false)] // time only, no date
    public void DateTime_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-04/schema#", "format": "date-time"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    [Fact]
    public void DateTime_NonStringInput_PassesValidation()
    {
        var schema = """{"$schema": "http://json-schema.org/draft-04/schema#", "format": "date-time"}""";

        // Numbers and other non-strings should pass format validation
        Assert.True(JsonSchemaValidator.Validate(schema, "123", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "true", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(schema, "null", _options).Valid);
    }

    #endregion

    #region Email Format

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("test+tag@example.co.uk", true)]
    [InlineData("user.name@example.com", true)]
    [InlineData("user@sub.example.com", true)]
    [InlineData("not-an-email", false)]
    [InlineData("@example.com", false)] // missing local part
    [InlineData("user@", false)] // missing domain
    [InlineData("user@@example.com", false)] // double @
    [InlineData("", false)] // empty string
    public void Email_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-04/schema#", "format": "email"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region Hostname Format

    [Theory]
    [InlineData("example.com", true)]
    [InlineData("sub.example.com", true)]
    [InlineData("a-b.example.co.uk", true)]
    [InlineData("localhost", true)]
    [InlineData("example", true)] // single label valid
    [InlineData("-invalid.com", false)] // starts with hyphen
    [InlineData("invalid-.com", false)] // ends with hyphen
    [InlineData("example..com", false)] // double dot
    [InlineData("", false)] // empty
    public void Hostname_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-04/schema#", "format": "hostname"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region IPv4 Format

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("0.0.0.0", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("256.1.1.1", false)] // 256 invalid
    [InlineData("192.168.1", false)] // missing octet
    [InlineData("192.168.1.1.1", false)] // extra octet
    [InlineData("192.168.1.a", false)] // non-numeric
    [InlineData("", false)] // empty
    [InlineData("::1", false)] // IPv6 not valid as IPv4
    public void IPv4_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-04/schema#", "format": "ipv4"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region IPv6 Format

    [Theory]
    [InlineData("2001:db8::1", true)]
    [InlineData("::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("2001:0db8:0000:0000:0000:0000:0000:0001", true)] // full form
    [InlineData("::ffff:192.168.1.1", true)] // IPv4-mapped
    [InlineData("192.168.1.1", false)] // IPv4 not valid as IPv6
    [InlineData("", false)] // empty
    [InlineData("invalid", false)]
    public void IPv6_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-04/schema#", "format": "ipv6"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region URI Format

    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("https://example.com/path?query=1", true)]
    [InlineData("ftp://ftp.example.com/file.txt", true)]
    [InlineData("urn:isbn:0451450523", true)]
    [InlineData("mailto:user@example.com", true)]
    [InlineData("/relative/path", false)] // relative not allowed in uri
    [InlineData("", false)] // empty
    public void Uri_Validation(string input, bool expected)
    {
        var schema = """{"$schema": "http://json-schema.org/draft-04/schema#", "format": "uri"}""";
        var instance = $"\"{input}\"";

        var result = JsonSchemaValidator.Validate(schema, instance, _options);

        Assert.Equal(expected, result.Valid);
    }

    #endregion

    #region FormatValidators Static API

    [Fact]
    public void FormatValidators_StaticMethods_ReturnCorrectResults()
    {
        using var validDateTime = JsonDocument.Parse("\"2023-12-25T10:30:45Z\"");
        using var invalidDateTime = JsonDocument.Parse("\"not-a-date\"");
        using var validEmail = JsonDocument.Parse("\"user@example.com\"");
        using var invalidEmail = JsonDocument.Parse("\"not-an-email\"");
        using var validIpv4 = JsonDocument.Parse("\"192.168.1.1\"");
        using var invalidIpv4 = JsonDocument.Parse("\"256.1.1.1\"");

        Assert.True(JsonSchemaValidation.Draft4.Keywords.Format.FormatValidators.IsValidDateTime(validDateTime.RootElement));
        Assert.False(JsonSchemaValidation.Draft4.Keywords.Format.FormatValidators.IsValidDateTime(invalidDateTime.RootElement));
        Assert.True(JsonSchemaValidation.Draft4.Keywords.Format.FormatValidators.IsValidEmail(validEmail.RootElement));
        Assert.False(JsonSchemaValidation.Draft4.Keywords.Format.FormatValidators.IsValidEmail(invalidEmail.RootElement));
        Assert.True(JsonSchemaValidation.Draft4.Keywords.Format.FormatValidators.IsValidIpv4(validIpv4.RootElement));
        Assert.False(JsonSchemaValidation.Draft4.Keywords.Format.FormatValidators.IsValidIpv4(invalidIpv4.RootElement));
    }

    #endregion
}
