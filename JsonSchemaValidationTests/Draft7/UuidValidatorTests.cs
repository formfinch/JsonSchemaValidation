// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Draft7;

/// <summary>
/// Comprehensive tests for UUID format validator (RFC 4122).
/// </summary>
public class UuidValidatorTests
{
    private readonly SchemaValidationOptions _options = new()
    {
        Draft7 = new Draft7Options { FormatAssertionEnabled = true }
    };

    private const string Schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "format": "uuid"}""";

    #region Valid UUIDs

    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")] // lowercase
    [InlineData("550E8400-E29B-41D4-A716-446655440000")] // uppercase
    [InlineData("550e8400-E29B-41d4-A716-446655440000")] // mixed case
    [InlineData("00000000-0000-0000-0000-000000000000")] // nil UUID
    [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")] // max UUID
    [InlineData("123e4567-e89b-12d3-a456-426614174000")] // version 1
    [InlineData("6ba7b810-9dad-11d1-80b4-00c04fd430c8")] // version 1 example
    [InlineData("f47ac10b-58cc-4372-a567-0e02b2c3d479")] // version 4
    public void ValidUuid_PassesValidation(string uuid)
    {
        var instance = $"\"{uuid}\"";

        var result = JsonSchemaValidator.Validate(Schema, instance, _options);

        Assert.True(result.Valid, $"UUID '{uuid}' should be valid");
    }

    #endregion

    #region Invalid UUIDs

    [Theory]
    [InlineData("550e8400e29b41d4a716446655440000")] // missing hyphens
    [InlineData("550e8400-e29b-41d4-a716-44665544000")] // too short
    [InlineData("550e8400-e29b-41d4-a716-4466554400000")] // too long
    [InlineData("550e8400-e29b-41d4-a716-44665544000g")] // invalid character 'g'
    [InlineData("{550e8400-e29b-41d4-a716-446655440000}")] // with braces
    [InlineData("(550e8400-e29b-41d4-a716-446655440000)")] // with parens
    [InlineData("550e8400-e29b41d4-a716-446655440000")] // wrong hyphen position
    [InlineData("")] // empty string
    [InlineData("not-a-uuid")] // random string
    public void InvalidUuid_FailsValidation(string uuid)
    {
        var instance = $"\"{uuid}\"";

        var result = JsonSchemaValidator.Validate(Schema, instance, _options);

        Assert.False(result.Valid, $"UUID '{uuid}' should be invalid");
    }

    #endregion

    #region Non-String Input

    [Fact]
    public void NonStringInput_PassesValidation()
    {
        // Format validators should pass for non-string types
        Assert.True(JsonSchemaValidator.Validate(Schema, "123", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(Schema, "true", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(Schema, "null", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(Schema, "{}", _options).Valid);
        Assert.True(JsonSchemaValidator.Validate(Schema, "[]", _options).Valid);
    }

    #endregion

    #region Direct Validator Tests

    [Fact]
    public void UuidValidator_DirectCall_WorksCorrectly()
    {
        using var validDoc = JsonDocument.Parse("\"550e8400-e29b-41d4-a716-446655440000\"");
        using var invalidDoc = JsonDocument.Parse("\"not-a-uuid\"");
        using var numberDoc = JsonDocument.Parse("123");

        var validator = new JsonSchemaValidation.Draft7.Keywords.Format.UuidValidator();

        Assert.True(validator.IsValid(validDoc.RootElement));
        Assert.False(validator.IsValid(invalidDoc.RootElement));
        Assert.True(validator.IsValid(numberDoc.RootElement)); // non-string passes
    }

    #endregion
}
