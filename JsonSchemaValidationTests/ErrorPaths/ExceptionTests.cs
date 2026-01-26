// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidationTests.ErrorPaths;

/// <summary>
/// Tests for exception handling and edge cases in error paths.
/// Covers InvalidSchemaException, ArgumentException, InvalidOperationException,
/// cleanup verification, and scope restoration.
/// </summary>
public class ExceptionTests
{
    #region InvalidSchemaException Tests

    [Fact]
    public void TryRegisterSchema_InvalidIdResolution_IsHandledGracefully()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        // Schema with $id that can't be resolved against a base URI
        using var doc = JsonDocument.Parse("""
            {
                "$id": "http://example.com/base",
                "$defs": {
                    "inner": {
                        "$id": "://invalid-uri",
                        "type": "string"
                    }
                }
            }
            """);

        // The library should handle invalid inner $id gracefully by either:
        // 1. Accepting the schema (ignoring/treating invalid $id as relative)
        // 2. Returning false (graceful rejection)
        // 3. Throwing a defined exception (explicit rejection)
        try
        {
            var result = schemaRepository.TryRegisterSchema(doc.RootElement, out var metadata);

            if (result)
            {
                // Success path: verify base schema was registered correctly
                Assert.NotNull(metadata);
                Assert.Equal(new Uri("http://example.com/base"), metadata.SchemaUri);
            }
            // else: graceful rejection via false return is acceptable
        }
        catch (Exception ex) when (ex is InvalidSchemaException or UriFormatException)
        {
            // Explicit rejection via exception is acceptable
        }
    }

    [Fact]
    public void PatternValidator_EmptyPattern_ThrowsInvalidSchemaException()
    {
        var schema = """{"pattern": ""}""";

        Assert.Throws<InvalidSchemaException>(() =>
            JsonSchemaValidator.Validate(schema, "\"test\""));
    }

    [Fact]
    public void InvalidSchemaException_HasMessage()
    {
        try
        {
            var schema = """{"pattern": ""}""";
            JsonSchemaValidator.Validate(schema, "\"test\"");
            Assert.Fail("Expected InvalidSchemaException");
        }
        catch (InvalidSchemaException ex)
        {
            Assert.False(string.IsNullOrEmpty(ex.Message));
            Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void InvalidSchemaException_CanBeCreatedWithInnerException()
    {
        var inner = new FormatException("inner error");
        var ex = new InvalidSchemaException("outer message", inner);

        Assert.Equal("outer message", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void InvalidSchemaException_DefaultConstructor_Works()
    {
        var ex = new InvalidSchemaException();
        Assert.NotNull(ex);
    }

    #endregion

    #region ArgumentException Tests

    [Fact]
    public void GetSchema_NullUri_ThrowsArgumentNullException()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        Assert.Throws<ArgumentNullException>(() =>
            schemaRepository.GetSchema(null!));
    }

    [Fact]
    public void GetSchema_NonExistentUri_ThrowsArgumentException()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        var ex = Assert.Throws<ArgumentException>(() =>
            schemaRepository.GetSchema(new Uri("http://example.com/does-not-exist")));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void GetSchema_NonExistentAnchor_ThrowsArgumentException()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        // Register a schema
        var schema = JsonDocument.Parse("""
            {
                "$id": "http://example.com/anchor-test",
                "type": "object"
            }
            """).RootElement;
        schemaRepository.TryRegisterSchema(schema, out _);

        // Try to get a non-existent anchor
        var ex = Assert.Throws<ArgumentException>(() =>
            schemaRepository.GetSchema(new Uri("http://example.com/anchor-test#missing")));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void GetSchema_InvalidJsonPointer_ThrowsInvalidOperationException()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        // Register a schema
        var schema = JsonDocument.Parse("""
            {
                "$id": "http://example.com/pointer-test",
                "type": "object",
                "properties": {
                    "name": {"type": "string"}
                }
            }
            """).RootElement;
        schemaRepository.TryRegisterSchema(schema, out _);

        // Try to navigate to a non-existent property via JSON pointer
        var ex = Assert.Throws<InvalidOperationException>(() =>
            schemaRepository.GetSchema(new Uri("http://example.com/pointer-test#/properties/missing")));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region InvalidOperationException Tests

    [Fact]
    public void TryRegisterSchema_IdWithFragment_ThrowsInvalidOperationException()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        var schema = JsonDocument.Parse("""
            {
                "$id": "http://example.com/test#fragment",
                "type": "string"
            }
            """).RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            schemaRepository.TryRegisterSchema(schema, out _));

        Assert.Contains("fragment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsJsonException()
    {
        // Parse should throw for invalid JSON
        Assert.ThrowsAny<JsonException>(() => JsonSchemaValidator.Parse("{invalid}"));
    }

    [Fact]
    public void Parse_NonSchemaValue_ReturnsWorkingSchema()
    {
        // Non-object/boolean values like numbers are not valid schemas per spec,
        // but the library may handle them gracefully
        try
        {
            var schema = JsonSchemaValidator.Parse("123");
            // If it doesn't throw, the schema should still be usable
            var result = schema.Validate("\"test\"");
            Assert.NotNull(result);
        }
        catch (InvalidOperationException)
        {
            // Library may reject non-schema values
        }
    }

    #endregion

    #region NotSupportedException Tests

    [Fact]
    public void Validate_DisabledDraft_ThrowsNotSupportedException()
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "type": "string"}""";
        var options = new SchemaValidationOptions { EnableDraft7 = false };

        var ex = Assert.Throws<NotSupportedException>(() =>
            JsonSchemaValidator.Validate(schema, "\"test\"", options));

        Assert.Contains("draft-07", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_DisabledDraft_ThrowsNotSupportedException()
    {
        var schema = """{"$schema": "https://json-schema.org/draft/2019-09/schema", "type": "string"}""";
        var options = new SchemaValidationOptions { EnableDraft201909 = false };

        var ex = Assert.Throws<NotSupportedException>(() =>
            JsonSchemaValidator.Parse(schema, options));

        Assert.Contains("2019-09", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-04/schema#", "EnableDraft4")]
    [InlineData("http://json-schema.org/draft-06/schema#", "EnableDraft6")]
    [InlineData("http://json-schema.org/draft-07/schema#", "EnableDraft7")]
    [InlineData("https://json-schema.org/draft/2019-09/schema", "EnableDraft201909")]
    [InlineData("https://json-schema.org/draft/2020-12/schema", "EnableDraft202012")]
    public void Validate_AllDraftsCanBeDisabled(string schemaUri, string optionName)
    {
        var schema = $$"""{"$schema": "{{schemaUri}}", "type": "string"}""";

        var options = new SchemaValidationOptions
        {
            EnableDraft3 = false,
            EnableDraft4 = false,
            EnableDraft6 = false,
            EnableDraft7 = false,
            EnableDraft201909 = false,
            EnableDraft202012 = false
        };

        // Re-enable just one that we're NOT testing to make sure we can still create the services
        options.EnableDraft202012 = optionName != "EnableDraft202012";

        if (optionName == "EnableDraft202012")
        {
            options.EnableDraft7 = true; // Enable something so services can be created
        }

        Assert.Throws<NotSupportedException>(() =>
            JsonSchemaValidator.Validate(schema, "\"test\"", options));
    }

    #endregion

    #region Scope and Cleanup Tests

    [Fact]
    public void RefValidator_AfterException_ScopeIsRestored()
    {
        // Test that validation scope is properly restored even if an error occurs
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var validatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        var contextFactory = serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        var schema = JsonDocument.Parse("""
            {
                "$id": "http://example.com/scope-test",
                "type": "object",
                "properties": {
                    "child": {"$ref": "#/$defs/child"}
                },
                "$defs": {
                    "child": {"type": "string"}
                }
            }
            """).RootElement;

        schemaRepository.TryRegisterSchema(schema, out var schemaData);
        var validator = validatorFactory.GetValidator(schemaData!.SchemaUri!);

        // First validation - invalid
        var invalidInstance = JsonDocument.Parse("""{"child": 123}""").RootElement;
        var context1 = contextFactory.CreateContextForRoot(invalidInstance);
        var result1 = validator.ValidateRoot(context1);
        Assert.False(result1.IsValid);

        // Second validation - valid (scope should be clean)
        var validInstance = JsonDocument.Parse("""{"child": "hello"}""").RootElement;
        var context2 = contextFactory.CreateContextForRoot(validInstance);
        var result2 = validator.ValidateRoot(context2);
        Assert.True(result2.IsValid);
    }

    [Fact]
    public void DynamicRef_AfterException_ScopeIsRestored()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var validatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        var contextFactory = serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        var schema = JsonDocument.Parse("""
            {
                "$id": "http://example.com/dynamic-scope-test",
                "$dynamicAnchor": "node",
                "type": "object",
                "properties": {
                    "value": {"type": "string"},
                    "child": {"$dynamicRef": "#node"}
                }
            }
            """).RootElement;

        schemaRepository.TryRegisterSchema(schema, out var schemaData);
        var validator = validatorFactory.GetValidator(schemaData!.SchemaUri!);

        // Multiple validations to verify scope cleanup
        for (int i = 0; i < 5; i++)
        {
            var instance = JsonDocument.Parse("""{"value": "test", "child": {"value": "nested"}}""").RootElement;
            var context = contextFactory.CreateContextForRoot(instance);
            var result = validator.ValidateRoot(context);
            Assert.True(result.IsValid, $"Iteration {i} failed");
        }
    }

    [Fact]
    public void NestedValidation_OnFailure_AllScopesRestored()
    {
        var schema = """
            {
                "allOf": [
                    {"type": "object"},
                    {
                        "properties": {
                            "items": {
                                "type": "array",
                                "items": {
                                    "allOf": [
                                        {"type": "object"},
                                        {"required": ["id"]}
                                    ]
                                }
                            }
                        }
                    }
                ]
            }
            """;

        // Invalid instance (missing required "id" in nested items)
        var invalidInstance = """{"items": [{"name": "test"}]}""";

        // Valid instance
        var validInstance = """{"items": [{"id": 1, "name": "test"}]}""";

        // First validation fails
        var result1 = JsonSchemaValidator.Validate(schema, invalidInstance);
        Assert.False(result1.Valid);

        // Second validation should succeed (clean state)
        var result2 = JsonSchemaValidator.Validate(schema, validInstance);
        Assert.True(result2.Valid);

        // Third validation of invalid should still fail correctly
        var result3 = JsonSchemaValidator.Validate(schema, invalidInstance);
        Assert.False(result3.Valid);
    }

    #endregion

    #region JsonException Tests

    [Theory]
    [InlineData("{invalid}")]
    [InlineData("[1, 2, }")]
    [InlineData("")]
    public void Validate_InvalidJsonSchema_ThrowsJsonException(string invalidJson)
    {
        // JsonReaderException inherits from JsonException
        Assert.ThrowsAny<JsonException>(() =>
            JsonSchemaValidator.Validate(invalidJson, "\"test\""));
    }

    [Theory]
    [InlineData("{invalid}")]
    [InlineData("[1, 2, }")]
    [InlineData("")]
    public void Validate_InvalidJsonInstance_ThrowsJsonException(string invalidJson)
    {
        // JsonReaderException inherits from JsonException
        Assert.ThrowsAny<JsonException>(() =>
            JsonSchemaValidator.Validate("""{"type": "string"}""", invalidJson));
    }

    [Theory]
    [InlineData("{invalid}")]
    [InlineData("[1, 2, }")]
    public void Parse_InvalidJsonSchema_ThrowsJsonException(string invalidJson)
    {
        // JsonReaderException inherits from JsonException
        Assert.ThrowsAny<JsonException>(() =>
            JsonSchemaValidator.Parse(invalidJson));
    }

    #endregion

    #region Constructor Exception Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void LruCache_InvalidCapacity_ThrowsArgumentOutOfRangeException(int invalidCapacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FormFinch.JsonSchemaValidation.Common.LruCache<string, string>(invalidCapacity));
    }

    [Fact]
    public void SchemaRepository_NullOptions_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddJsonSchemaValidation((SchemaValidationOptions)null!));
    }

    #endregion

    #region Recovery Tests

    [Fact]
    public void AfterInvalidSchema_SubsequentValidationsWork()
    {
        // Try to validate with an invalid regex pattern
        try
        {
            JsonSchemaValidator.Validate("""{"pattern": "(unclosed"}""", "\"test\"");
        }
        catch
        {
            // Expected
        }

        // Subsequent valid schema should still work
        var result = JsonSchemaValidator.Validate("""{"type": "string"}""", "\"test\"");
        Assert.True(result.Valid);
    }

    [Fact]
    public void AfterInvalidJson_SubsequentValidationsWork()
    {
        // Try to validate invalid JSON
        try
        {
            JsonSchemaValidator.Validate("""{"type": "string"}""", "{invalid}");
        }
        catch (JsonException)
        {
            // Expected
        }

        // Subsequent valid JSON should still work
        var result = JsonSchemaValidator.Validate("""{"type": "string"}""", "\"test\"");
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
