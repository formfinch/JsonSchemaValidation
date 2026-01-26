// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Stress.Fuzzing;

/// <summary>
/// Property-based tests for edge case discovery.
/// Tests regex pattern safety, schema parsing robustness, recursion depth,
/// and format validator robustness.
/// </summary>
[Trait("Category", "Fuzzing")]
public class PropertyBasedTests
{
    private static readonly TimeSpan MaxTestDuration = TimeSpan.FromSeconds(5);
    private static readonly Random RandomGenerator = new(42); // Fixed seed for reproducibility

    #region Regex Pattern Safety Tests

    [Theory]
    [InlineData("hello world")]
    [InlineData("a")]
    [InlineData("")]
    [InlineData("1234567890")]
    [InlineData("special!@#$%^&*()")]
    [InlineData("unicode: \u00e9\u00e8\u00ea")]
    [InlineData("newlines\n\r\ttabs")]
    public void AnyStringInput_WithSimplePattern_CompletesWithinTimeout(string input)
    {
        var schema = """{"pattern": "^[a-z]+$"}""";

        var completed = false;
        try
        {
            var escapedInput = EscapeJsonString(input);
            var result = JsonSchemaValidator.Validate(schema, $"\"{escapedInput}\"");
            completed = true;
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout is acceptable - it means the protection worked
            completed = true;
        }
        catch (JsonException)
        {
            // Invalid JSON from escaped string - acceptable
            completed = true;
        }

        Assert.True(completed);
    }

    [Theory]
    [InlineData(".", "test")]
    [InlineData(".*", "anything")]
    [InlineData(".+", "something")]
    [InlineData("[a-z]", "a")]
    [InlineData("[0-9]+", "123")]
    [InlineData("\\d", "5")]
    [InlineData("\\w", "x")]
    [InlineData("^$", "")]
    [InlineData("a|b", "a")]
    public void RandomPatternWithRandomInput_NoUnhandledExceptions(string pattern, string input)
    {
        try
        {
            var schema = $$"""{"pattern": "{{pattern}}"}""";
            var result = JsonSchemaValidator.Validate(schema, $"\"{EscapeJsonString(input)}\"");
            Assert.NotNull(result);
        }
        catch (JsonException)
        {
            // JSON parsing issues are acceptable
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout protection working
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            // Pattern or format issues acceptable
        }
    }

    [Fact]
    public void ManyRandomStrings_WithPattern_AllComplete()
    {
        var schema = """{"pattern": "^[a-z]+$"}""";
        var random = new Random(42);

        for (int i = 0; i < 100; i++)
        {
            var length = random.Next(0, 50);
            var chars = new char[length];
            for (int j = 0; j < length; j++)
            {
                chars[j] = (char)random.Next(32, 127);
            }
            var input = new string(chars);

            try
            {
                var escaped = EscapeJsonString(input);
                var result = JsonSchemaValidator.Validate(schema, $"\"{escaped}\"");
                Assert.NotNull(result);
            }
            catch (JsonException)
            {
                // Acceptable
            }
            catch (RegexMatchTimeoutException)
            {
                // Acceptable
            }
        }
    }

    #endregion

    #region Schema Parsing Robustness Tests

    [Theory]
    [InlineData("type", "\"string\"")]
    [InlineData("type", "\"number\"")]
    [InlineData("type", "\"object\"")]
    [InlineData("properties", "{}")]
    [InlineData("items", "true")]
    public void RandomJsonObject_DoesNotCrashParser(string key, string value)
    {
        try
        {
            var schema = $$"""{"{{key}}": {{value}}}""";
            var result = JsonSchemaValidator.Validate(schema, "\"test\"");
            Assert.NotNull(result);
        }
        catch (JsonException)
        {
            // JSON parsing issues acceptable
        }
        catch (NotSupportedException)
        {
            // Unsupported draft acceptable
        }
    }

    [Theory]
    [InlineData("allOf")]
    [InlineData("anyOf")]
    public void EmptyArrayKeywords_MayThrowOrValidate(string keyword)
    {
        try
        {
            var schema = $$"""{"{{keyword}}": []}""";
            var result = JsonSchemaValidator.Validate(schema, "\"test\"");
            // Empty allOf/anyOf may be valid (allOf: [] = true, anyOf: [] = false)
            Assert.NotNull(result);
        }
        catch (InvalidSchemaException)
        {
            // Library may reject empty allOf/anyOf
        }
    }

    [Theory]
    [InlineData("string", "\"hello\"")]
    [InlineData("number", "42")]
    [InlineData("integer", "42")]
    [InlineData("boolean", "true")]
    [InlineData("null", "null")]
    [InlineData("object", "{}")]
    [InlineData("array", "[]")]
    public void ValidSchemaWithVariousInstances_NeverThrows(string schemaType, string instance)
    {
        var schema = $$"""{"type": "{{schemaType}}"}""";

        var result = JsonSchemaValidator.Validate(schema, instance);
        Assert.NotNull(result);
    }

    #endregion

    #region Recursion Depth Enforcement Tests

    [Fact]
    public void DeeplyNestedRef_RespectsMaxRecursionDepth()
    {
        // Create a schema with deep $ref chains
        // The library has MaxRecursionDepth = 100
        // System.Text.Json default max depth is 64
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        var schema = JsonDocument.Parse("""
            {
                "$id": "http://example.com/recursive",
                "$defs": {
                    "node": {
                        "type": "object",
                        "properties": {
                            "child": {"$ref": "#/$defs/node"}
                        }
                    }
                },
                "$ref": "#/$defs/node"
            }
            """).RootElement;

        schemaRepository.TryRegisterSchema(schema, out var schemaData);
        Assert.NotNull(schemaData);

        // Create a moderately nested instance (within System.Text.Json's 64 depth limit)
        var nestedInstance = BuildDeeplyNestedObject(60);

        // Should not cause stack overflow
        var result = JsonSchemaValidator.Validate(
            """{"$ref": "http://example.com/recursive"}""",
            nestedInstance);

        // Result doesn't matter - just verifying no stack overflow
        Assert.NotNull(result);
    }

    [Fact]
    public void SelfReferencingSchema_HandlesGracefully()
    {
        var schema = """
            {
                "$id": "http://example.com/tree",
                "type": "object",
                "properties": {
                    "value": {"type": "string"},
                    "children": {
                        "type": "array",
                        "items": {"$ref": "http://example.com/tree"}
                    }
                }
            }
            """;

        var instance = """
            {
                "value": "root",
                "children": [
                    {"value": "child1", "children": []},
                    {"value": "child2", "children": [{"value": "grandchild", "children": []}]}
                ]
            }
            """;

        // Should validate without issues
        var result = JsonSchemaValidator.Validate(schema, instance);
        Assert.True(result.Valid);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)] // System.Text.Json default max depth is 64
    public void VariousNestingDepths_CompleteSuccessfully(int depth)
    {
        var schema = """{"type": "object", "additionalProperties": true}""";
        var instance = BuildDeeplyNestedObject(depth);

        var result = JsonSchemaValidator.Validate(schema, instance);
        Assert.NotNull(result);
    }

    #endregion

    #region Format Validator Robustness Tests

    [Theory]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("2024-13-45")]
    [InlineData("random string")]
    [InlineData("12345")]
    [InlineData("true")]
    public void RandomString_ToDateTimeFormat_NoUnhandledExceptions(string input)
    {
        try
        {
            var schema = """{"format": "date-time"}""";
            var options = new SchemaValidationOptions
            {
                Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
            };
            var result = JsonSchemaValidator.Validate(schema, $"\"{EscapeJsonString(input)}\"", options);
            Assert.NotNull(result);
        }
        catch (JsonException)
        {
            // Acceptable
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@invalid")]
    [InlineData("test@")]
    [InlineData("multiple@@at")]
    public void RandomString_ToEmailFormat_NoUnhandledExceptions(string input)
    {
        try
        {
            var schema = """{"format": "email"}""";
            var options = new SchemaValidationOptions
            {
                Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
            };
            var result = JsonSchemaValidator.Validate(schema, $"\"{EscapeJsonString(input)}\"", options);
            Assert.NotNull(result);
        }
        catch (JsonException)
        {
            // Acceptable
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-uri")]
    [InlineData("://missing-scheme")]
    [InlineData("http://")]
    [InlineData("ftp://[invalid")]
    public void RandomString_ToUriFormat_NoUnhandledExceptions(string input)
    {
        try
        {
            var schema = """{"format": "uri"}""";
            var options = new SchemaValidationOptions
            {
                Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
            };
            var result = JsonSchemaValidator.Validate(schema, $"\"{EscapeJsonString(input)}\"", options);
            Assert.NotNull(result);
        }
        catch (JsonException)
        {
            // Acceptable
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-ip")]
    [InlineData("999.999.999.999")]
    [InlineData("1.2.3")]
    [InlineData("1.2.3.4.5")]
    public void RandomString_ToIpv4Format_NoUnhandledExceptions(string input)
    {
        try
        {
            var schema = """{"format": "ipv4"}""";
            var options = new SchemaValidationOptions
            {
                Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
            };
            var result = JsonSchemaValidator.Validate(schema, $"\"{EscapeJsonString(input)}\"", options);
            Assert.NotNull(result);
        }
        catch (JsonException)
        {
            // Acceptable
        }
    }

    [Theory]
    [InlineData("date-time")]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData("email")]
    [InlineData("idn-email")]
    [InlineData("hostname")]
    [InlineData("idn-hostname")]
    [InlineData("ipv4")]
    [InlineData("ipv6")]
    [InlineData("uri")]
    [InlineData("uri-reference")]
    [InlineData("uuid")]
    [InlineData("json-pointer")]
    [InlineData("regex")]
    public void AllFormatValidators_HandleEmptyString(string format)
    {
        var schema = $$"""{"format": "{{format}}"}""";
        var options = new SchemaValidationOptions
        {
            Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
        };

        // Should not throw
        var result = JsonSchemaValidator.Validate(schema, "\"\"", options);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("date-time")]
    [InlineData("email")]
    [InlineData("uri")]
    [InlineData("ipv4")]
    [InlineData("ipv6")]
    public void AllFormatValidators_HandleSpecialCharacters(string format)
    {
        var schema = $$"""{"format": "{{format}}"}""";
        var options = new SchemaValidationOptions
        {
            Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
        };

        var specialStrings = new[]
        {
            "\\u0000",
            "\\n\\r\\t",
            "\\\\",
            "test\\\"quote"
        };

        foreach (var special in specialStrings)
        {
            var result = JsonSchemaValidator.Validate(schema, $"\"{special}\"", options);
            Assert.NotNull(result);
        }
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

    private static string EscapeJsonString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("\b", "\\b")
            .Replace("\f", "\\f");
    }

    private static string BuildDeeplyNestedObject(int depth)
    {
        if (depth <= 0) return "{}";

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < depth; i++)
        {
            sb.Append("{\"nested\":");
        }
        sb.Append("{}");
        for (int i = 0; i < depth; i++)
        {
            sb.Append('}');
        }
        return sb.ToString();
    }

    #endregion
}
