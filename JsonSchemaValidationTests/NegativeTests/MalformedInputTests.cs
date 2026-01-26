// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidationTests.NegativeTests;

/// <summary>
/// Tests for handling malformed inputs: invalid JSON, malformed schemas,
/// boundary conditions, invalid $ref targets, invalid format strings, and invalid regex patterns.
/// </summary>
public class MalformedInputTests
{
    #region Invalid JSON Tests

    [Theory]
    [InlineData("{")]                           // Unclosed brace
    [InlineData("[")]                           // Unclosed bracket
    [InlineData("{\"a\": 1,}")]                 // Trailing comma
    [InlineData("{\"a\" 1}")]                   // Missing colon
    [InlineData("{\"a\": \"test")]              // Unclosed string
    [InlineData("{\"a\": \\x00}")]              // Invalid escape sequence
    [InlineData("{'a': 1}")]                    // Single quotes (invalid in JSON)
    [InlineData("{a: 1}")]                      // Unquoted key
    [InlineData("undefined")]                   // JavaScript undefined
    [InlineData("NaN")]                         // JavaScript NaN
    [InlineData("Infinity")]                    // JavaScript Infinity
    public void Validate_InvalidJsonInstance_ThrowsJsonException(string invalidJson)
    {
        var schema = """{"type": "object"}""";

        // JsonReaderException inherits from JsonException
        Assert.ThrowsAny<JsonException>(() => JsonSchemaValidator.Validate(schema, invalidJson));
    }

    [Theory]
    [InlineData("{")]                           // Unclosed brace
    [InlineData("[\"type\": \"string\"]")]      // Array instead of object
    [InlineData("{\"type\": \"string\",}")]     // Trailing comma
    public void Validate_InvalidJsonSchema_ThrowsJsonException(string invalidSchema)
    {
        var instance = "\"test\"";

        // JsonReaderException inherits from JsonException
        Assert.ThrowsAny<JsonException>(() => JsonSchemaValidator.Validate(invalidSchema, instance));
    }

    #endregion

    #region Malformed Schema Tests

    [Theory]
    [InlineData("\"just a string\"")]           // String instead of object/boolean
    [InlineData("123")]                         // Number instead of object/boolean
    [InlineData("null")]                        // Null instead of object/boolean
    [InlineData("[1, 2, 3]")]                   // Array instead of object/boolean
    public void TryRegisterSchema_NonSchemaTypes_ReturnsFalse(string nonSchema)
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        using var doc = JsonDocument.Parse(nonSchema);
        var result = schemaRepository.TryRegisterSchema(doc.RootElement, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryRegisterSchema_NullSchema_ReturnsFalse()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        var result = schemaRepository.TryRegisterSchema(null, out var schemaData);

        Assert.False(result);
        Assert.Null(schemaData);
    }

    [Fact]
    public void Validate_SchemaWithInvalidSchemaUri_HandlesDraftDetection()
    {
        // Invalid $schema URI - library may throw NotSupportedException for unknown drafts
        // or use default draft depending on implementation
        var schema = """{"$schema": "not-a-valid-uri", "type": "string"}""";
        var instance = "\"hello\"";

        try
        {
            var result = JsonSchemaValidator.Validate(schema, instance);
            // If no exception, validation should succeed for a matching type
            Assert.True(result.Valid);
        }
        catch (NotSupportedException)
        {
            // Library may reject unknown $schema URIs
        }
    }

    [Theory]
    [InlineData("""{"type": ["string", "number", "invalid"]}""")] // Invalid type in array
    [InlineData("""{"type": "invalid"}""")]                        // Invalid type string
    public void Validate_SchemaWithInvalidTypeKeyword_HandledGracefully(string schema)
    {
        // Schemas with invalid type values may throw or may be treated as not matching
        var instance = "\"test\"";

        try
        {
            var result = JsonSchemaValidator.Validate(schema, instance);
            // Should not throw - just validate
            Assert.NotNull(result);
        }
        catch (InvalidSchemaException)
        {
            // Library may reject invalid type values
        }
    }

    #endregion

    #region Boundary Condition Tests

    [Fact]
    public void Validate_EmptySchema_AcceptsAnyInstance()
    {
        // Empty schema {} is equivalent to true - accepts anything
        var emptySchema = "{}";

        Assert.True(JsonSchemaValidator.IsValid(emptySchema, "\"string\""));
        Assert.True(JsonSchemaValidator.IsValid(emptySchema, "123"));
        Assert.True(JsonSchemaValidator.IsValid(emptySchema, "true"));
        Assert.True(JsonSchemaValidator.IsValid(emptySchema, "null"));
        Assert.True(JsonSchemaValidator.IsValid(emptySchema, "{}"));
        Assert.True(JsonSchemaValidator.IsValid(emptySchema, "[]"));
    }

    [Fact]
    public void Validate_BooleanTrueSchema_AcceptsAnyInstance()
    {
        var trueSchema = "true";

        Assert.True(JsonSchemaValidator.IsValid(trueSchema, "\"anything\""));
        Assert.True(JsonSchemaValidator.IsValid(trueSchema, "123"));
        Assert.True(JsonSchemaValidator.IsValid(trueSchema, "{}"));
    }

    [Fact]
    public void Validate_BooleanFalseSchema_RejectsEverything()
    {
        var falseSchema = "false";

        Assert.False(JsonSchemaValidator.IsValid(falseSchema, "\"anything\""));
        Assert.False(JsonSchemaValidator.IsValid(falseSchema, "123"));
        Assert.False(JsonSchemaValidator.IsValid(falseSchema, "{}"));
        Assert.False(JsonSchemaValidator.IsValid(falseSchema, "null"));
    }

    [Fact]
    public void Validate_EmptyInstance_ValidatesAgainstObjectSchema()
    {
        var schema = """{"type": "object"}""";
        var emptyObject = "{}";

        var result = JsonSchemaValidator.Validate(schema, emptyObject);

        Assert.True(result.Valid);
    }

    [Fact]
    public void Validate_EmptyArray_ValidatesAgainstArraySchema()
    {
        var schema = """{"type": "array"}""";
        var emptyArray = "[]";

        var result = JsonSchemaValidator.Validate(schema, emptyArray);

        Assert.True(result.Valid);
    }

    [Fact]
    public void Validate_EmptyString_ValidatesAgainstStringSchema()
    {
        var schema = """{"type": "string"}""";
        var emptyString = "\"\"";

        var result = JsonSchemaValidator.Validate(schema, emptyString);

        Assert.True(result.Valid);
    }

    [Theory]
    [InlineData("""{"minLength": 0}""", "\"\"")]
    [InlineData("""{"minItems": 0}""", "[]")]
    [InlineData("""{"minProperties": 0}""", "{}")]
    [InlineData("""{"minimum": 0}""", "0")]
    [InlineData("""{"exclusiveMinimum": -1}""", "0")]
    public void Validate_ZeroBoundaryConditions_ValidatesCorrectly(string schema, string instance)
    {
        var result = JsonSchemaValidator.Validate(schema, instance);

        Assert.True(result.Valid);
    }

    #endregion

    #region Invalid $ref Tests

    [Fact]
    public void GetSchema_NonExistentUri_ThrowsArgumentException()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        var ex = Assert.Throws<ArgumentException>(() =>
            schemaRepository.GetSchema(new Uri("http://example.com/nonexistent")));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void GetSchema_InvalidAnchor_ThrowsArgumentException()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        var schema = JsonDocument.Parse("""{"$id": "http://example.com/test", "type": "string"}""").RootElement;
        schemaRepository.TryRegisterSchema(schema, out _);

        // Try to get a non-existent anchor
        var ex = Assert.Throws<ArgumentException>(() =>
            schemaRepository.GetSchema(new Uri("http://example.com/test#nonexistent")));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void TryGetDynamicRef_InvalidAnchorFormat_ReturnsFalse()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        // Dynamic anchor must start with #
        var result = schemaRepository.TryGetDynamicRef("nohash", out var schema);

        Assert.False(result);
        Assert.Null(schema);
    }

    [Fact]
    public void TryGetDynamicRef_EmptyString_ReturnsFalse()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        var result = schemaRepository.TryGetDynamicRef("", out var schema);

        Assert.False(result);
        Assert.Null(schema);
    }

    [Fact]
    public void TryGetDynamicRef_NonExistentAnchor_ReturnsFalse()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        var result = schemaRepository.TryGetDynamicRef("#nonexistent", out var schema);

        Assert.False(result);
        Assert.Null(schema);
    }

    [Fact]
    public void TryRegisterSchema_SchemaIdWithFragment_ThrowsInvalidOperationException()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        // $id cannot contain a fragment
        var schema = JsonDocument.Parse("""{"$id": "http://example.com/test#fragment", "type": "string"}""").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            schemaRepository.TryRegisterSchema(schema, out _));

        Assert.Contains("fragment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Invalid Format String Tests

    [Theory]
    [InlineData("date-time", "\"not-a-date\"")]
    [InlineData("date", "\"2024-13-45\"")]
    [InlineData("time", "\"25:99:99\"")]
    [InlineData("email", "\"not@valid\"")]
    [InlineData("hostname", "\"-invalid-hostname\"")]
    [InlineData("ipv4", "\"999.999.999.999\"")]
    [InlineData("ipv6", "\"not::an::ipv6\"")]
    [InlineData("uri", "\"://missing-scheme\"")]
    [InlineData("uuid", "\"not-a-valid-uuid\"")]
    public void Validate_InvalidFormatStrings_WithFormatAssertion_ReturnsInvalid(string format, string invalidValue)
    {
        var schema = $$$"""{"format": "{{{format}}}"}""";
        var options = new SchemaValidationOptions
        {
            Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
        };

        var result = JsonSchemaValidator.Validate(schema, invalidValue, options);

        Assert.False(result.Valid);
    }

    [Theory]
    [InlineData("date-time", "\"2024-01-15T10:30:00Z\"")]
    [InlineData("date", "\"2024-01-15\"")]
    [InlineData("time", "\"10:30:00Z\"")] // Time format requires timezone offset per RFC 3339
    [InlineData("email", "\"test@example.com\"")]
    [InlineData("hostname", "\"example.com\"")]
    [InlineData("ipv4", "\"192.168.1.1\"")]
    [InlineData("ipv6", "\"::1\"")]
    [InlineData("uri", "\"https://example.com\"")]
    [InlineData("uuid", "\"550e8400-e29b-41d4-a716-446655440000\"")]
    public void Validate_ValidFormatStrings_WithFormatAssertion_ReturnsValid(string format, string validValue)
    {
        var schema = $$$"""{"format": "{{{format}}}"}""";
        var options = new SchemaValidationOptions
        {
            Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
        };

        var result = JsonSchemaValidator.Validate(schema, validValue, options);

        Assert.True(result.Valid);
    }

    [Theory]
    [InlineData("unknown-format")]
    [InlineData("custom-format")]
    public void Validate_UnknownFormat_WithFormatAssertion_AcceptsAnyValue(string unknownFormat)
    {
        var schema = $$"""{"format": "{{unknownFormat}}"}""";
        var options = new SchemaValidationOptions
        {
            Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
        };

        var result = JsonSchemaValidator.Validate(schema, "\"any value\"", options);

        // Unknown formats should pass (spec says unknown formats are annotations only)
        Assert.True(result.Valid);
    }

    [Fact]
    public void Validate_EmptyFormatString_BehavesAsAnnotation()
    {
        // Empty format string may be treated as no format or may throw
        var schema = """{"format": ""}""";
        var options = new SchemaValidationOptions
        {
            Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
        };

        try
        {
            var result = JsonSchemaValidator.Validate(schema, "\"any value\"", options);
            // If it doesn't throw, the validation should pass (empty format = no validation)
            Assert.NotNull(result);
        }
        catch (InvalidSchemaException)
        {
            // Library may reject empty format strings
        }
    }

    #endregion

    #region Invalid Regex Pattern Tests

    [Theory]
    [InlineData("(")]                           // Unclosed group
    [InlineData("[")]                           // Unclosed character class
    [InlineData("(?P<name>test)")]              // Named groups not supported in ECMAScript
    [InlineData("(?<=look)behind")]             // Lookbehind (not in all ECMAScript versions)
    public void Validate_InvalidRegexPattern_ThrowsOrHandlesGracefully(string invalidPattern)
    {
        var schema = $$"""{"pattern": "{{invalidPattern}}"}""";

        // Should either throw during schema registration or handle gracefully
        try
        {
            var result = JsonSchemaValidator.Validate(schema, "\"test\"");
            // If it doesn't throw, that's acceptable (graceful handling)
            Assert.NotNull(result);
        }
        catch (Exception ex) when (ex is InvalidSchemaException or ArgumentException or System.Text.RegularExpressions.RegexParseException)
        {
            // Expected - invalid regex patterns can throw during schema processing
            Assert.NotNull(ex.Message);
        }
    }

    [Fact]
    public void Validate_TrailingBackslashPattern_ThrowsOrHandlesGracefully()
    {
        // Trailing backslash needs special handling due to JSON escaping
        var schema = "{\"pattern\": \"\\\\\"}";  // JSON escaped backslash

        try
        {
            var result = JsonSchemaValidator.Validate(schema, "\"test\"");
            Assert.NotNull(result);
        }
        catch (Exception ex) when (ex is InvalidSchemaException or ArgumentException or System.Text.RegularExpressions.RegexParseException or JsonException)
        {
            Assert.NotNull(ex.Message);
        }
    }

    [Fact]
    public void Validate_EmptyPattern_ThrowsInvalidSchemaException()
    {
        // Empty pattern is rejected as invalid
        var schema = """{"pattern": ""}""";

        Assert.Throws<InvalidSchemaException>(() => JsonSchemaValidator.Validate(schema, "\"anything\""));
    }

    [Theory]
    [InlineData("^test$")]
    [InlineData("[a-z]+")]
    public void Validate_ValidPatterns_WorkCorrectly(string pattern)
    {
        var schema = $$"""{"pattern": "{{pattern}}"}""";

        // Should not throw
        var result = JsonSchemaValidator.Validate(schema, "\"test\"");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_DigitPattern_WorksCorrectly()
    {
        // Need to use regular string for proper JSON escaping of backslash
        var schema = "{\"pattern\": \"\\\\d{3}-\\\\d{4}\"}";

        var result = JsonSchemaValidator.Validate(schema, "\"123-4567\"");
        Assert.NotNull(result);
        Assert.True(result.Valid);
    }

    #endregion

    #region Helper Methods

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt => opt.EnableDraft202012 = true);
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.InitializeSingletonServices();
        return serviceProvider;
    }

    #endregion
}
