// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Common;

/// <summary>
/// Tests for annotation-only keyword support (title, description, default, etc.).
/// Verifies that these keywords produce annotations in Detailed output without affecting validation.
/// </summary>
/// <remarks>
/// Tests use the options overload of <see cref="JsonSchemaValidator.Validate(string, string, SchemaValidationOptions, OutputFormat)"/>
/// to create isolated service providers, avoiding interference from parallel test classes
/// that share the default static service provider.
/// </remarks>
public class AnnotationKeywordTests
{
    // Fresh options for each test avoids sharing state with parallel test classes
    private static SchemaValidationOptions DefaultOptions => new();

    #region Annotation emission — Draft 2020-12

    [Theory]
    [InlineData("title", "\"My Title\"", "My Title")]
    [InlineData("description", "\"A description\"", "A description")]
    public void Draft202012_StringAnnotation_EmitsValueInDetailedOutput(string keyword, string jsonValue, string expected)
    {
        var schema = $$"""{"type": "string", "{{keyword}}": {{jsonValue}}}""";
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, keyword);
        Assert.NotNull(annotation);
        Assert.Equal(expected, GetAnnotationValue<string>(annotation, keyword));
    }

    [Fact]
    public void Draft202012_DefaultWithString_EmitsAnnotation()
    {
        var schema = """{"type": "string", "default": "fallback"}""";
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "default");
        Assert.NotNull(annotation);
        Assert.Equal("fallback", GetAnnotationValue<string>(annotation, "default"));
    }

    [Fact]
    public void Draft202012_DefaultWithNumber_EmitsAnnotation()
    {
        var schema = """{"type": "number", "default": 42}""";
        var result = JsonSchemaValidator.Validate(schema, "1", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "default");
        Assert.NotNull(annotation);
        var value = GetAnnotationJsonElement(annotation, "default");
        Assert.Equal(42, value.GetInt32());
    }

    [Fact]
    public void Draft202012_DefaultWithBoolean_EmitsAnnotation()
    {
        var schema = """{"type": "boolean", "default": true}""";
        var result = JsonSchemaValidator.Validate(schema, "false", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "default");
        Assert.NotNull(annotation);
        Assert.True(GetAnnotationValue<bool>(annotation, "default"));
    }

    [Fact]
    public void Draft202012_DefaultWithNull_EmitsAnnotation()
    {
        var schema = """{"default": null}""";
        var result = JsonSchemaValidator.Validate(schema, "\"anything\"", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "default");
        Assert.NotNull(annotation);
        Assert.Null(GetAnnotationValue<object>(annotation, "default"));
    }

    [Fact]
    public void Draft202012_DefaultWithArray_EmitsAnnotation()
    {
        var schema = """{"type": "array", "default": [1, 2, 3]}""";
        var result = JsonSchemaValidator.Validate(schema, "[]", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "default");
        Assert.NotNull(annotation);
        var value = GetAnnotationJsonElement(annotation, "default");
        Assert.Equal(JsonValueKind.Array, value.ValueKind);
        Assert.Equal(3, value.GetArrayLength());
    }

    [Fact]
    public void Draft202012_DefaultWithObject_EmitsAnnotation()
    {
        var schema = """{"type": "object", "default": {"key": "value"}}""";
        var result = JsonSchemaValidator.Validate(schema, "{}", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "default");
        Assert.NotNull(annotation);
        var value = GetAnnotationJsonElement(annotation, "default");
        Assert.Equal(JsonValueKind.Object, value.ValueKind);
    }

    [Fact]
    public void Draft202012_Examples_EmitsAnnotation()
    {
        var schema = """{"type": "string", "examples": ["foo", "bar"]}""";
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "examples");
        Assert.NotNull(annotation);
        var value = GetAnnotationJsonElement(annotation, "examples");
        Assert.Equal(JsonValueKind.Array, value.ValueKind);
        Assert.Equal(2, value.GetArrayLength());
    }

    [Fact]
    public void Draft202012_ReadOnly_EmitsAnnotation()
    {
        var schema = """{"type": "string", "readOnly": true}""";
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "readOnly");
        Assert.NotNull(annotation);
        Assert.True(GetAnnotationValue<bool>(annotation, "readOnly"));
    }

    [Fact]
    public void Draft202012_WriteOnly_EmitsAnnotation()
    {
        var schema = """{"type": "string", "writeOnly": true}""";
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "writeOnly");
        Assert.NotNull(annotation);
        Assert.True(GetAnnotationValue<bool>(annotation, "writeOnly"));
    }

    [Fact]
    public void Draft202012_Deprecated_EmitsAnnotation()
    {
        var schema = """{"type": "string", "deprecated": true}""";
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "deprecated");
        Assert.NotNull(annotation);
        Assert.True(GetAnnotationValue<bool>(annotation, "deprecated"));
    }

    #endregion

    #region $comment — must NOT produce annotations

    [Fact]
    public void Draft202012_Comment_DoesNotProduceAnnotation()
    {
        var schema = """{"type": "string", "$comment": "This is a comment"}""";
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "$comment");
        Assert.Null(annotation);
    }

    [Fact]
    public void Draft7_Comment_DoesNotProduceAnnotation()
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "type": "string", "$comment": "This is a comment"}""";
        var options = new SchemaValidationOptions { EnableDraft7 = true };
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", options, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "$comment");
        Assert.Null(annotation);
    }

    #endregion

    #region Annotations don't affect validation

    [Fact]
    public void AnnotationKeywords_DoNotAffectValidation_InvalidInstanceStaysInvalid()
    {
        var schema = """{"type": "string", "title": "Name", "description": "A name", "default": "John"}""";
        var result = JsonSchemaValidator.Validate(schema, "42", DefaultOptions);

        Assert.False(result.Valid);
    }

    [Fact]
    public void AnnotationKeywords_DoNotAffectValidation_ValidInstanceStaysValid()
    {
        var schema = """{"type": "string", "title": "Name", "description": "A name", "default": "John", "deprecated": true, "readOnly": true, "writeOnly": false, "examples": ["Alice", "Bob"]}""";
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", DefaultOptions);

        Assert.True(result.Valid);
    }

    #endregion

    #region Cross-draft annotation support

    [Fact]
    public void Draft7_TitleAndDescription_EmitAnnotations()
    {
        var schema = """{"$schema": "http://json-schema.org/draft-07/schema#", "type": "string", "title": "Name", "description": "A name field"}""";
        var options = new SchemaValidationOptions { EnableDraft7 = true };
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", options, OutputFormat.Detailed);

        Assert.True(result.Valid);
        Assert.NotNull(FindAnnotation(result, "title"));
        Assert.NotNull(FindAnnotation(result, "description"));
    }

    [Fact]
    public void Draft6_Examples_EmitsAnnotation()
    {
        var schema = """{"$schema": "http://json-schema.org/draft-06/schema#", "type": "string", "examples": ["foo"]}""";
        var options = new SchemaValidationOptions { EnableDraft6 = true };
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", options, OutputFormat.Detailed);

        Assert.True(result.Valid);
        Assert.NotNull(FindAnnotation(result, "examples"));
    }

    [Fact]
    public void Draft4_TitleAndDefault_EmitAnnotations()
    {
        var schema = """{"$schema": "http://json-schema.org/draft-04/schema#", "type": "string", "title": "Name", "default": "anon"}""";
        var options = new SchemaValidationOptions { EnableDraft4 = true };
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", options, OutputFormat.Detailed);

        Assert.True(result.Valid);
        Assert.NotNull(FindAnnotation(result, "title"));
        Assert.NotNull(FindAnnotation(result, "default"));
    }

    [Fact]
    public void Draft3_TitleDescriptionDefault_EmitAnnotations()
    {
        var schema = """{"$schema": "http://json-schema.org/draft-03/schema#", "type": "string", "title": "Name", "description": "Desc", "default": "x"}""";
        var options = new SchemaValidationOptions { EnableDraft3 = true };
        var result = JsonSchemaValidator.Validate(schema, "\"hello\"", options, OutputFormat.Detailed);

        Assert.True(result.Valid);
        Assert.NotNull(FindAnnotation(result, "title"));
        Assert.NotNull(FindAnnotation(result, "description"));
        Assert.NotNull(FindAnnotation(result, "default"));
    }

    #endregion

    #region Custom annotation keyword via public API

    [Fact]
    public void AddAnnotationKeyword_CustomKeyword_EmitsAnnotation()
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation();
        services.AddAnnotationKeyword("x-display-order");

        using var provider = services.BuildServiceProvider();
        provider.InitializeSingletonServices();

        var repo = provider.GetRequiredService<ISchemaRepository>();
        var validatorFactory = provider.GetRequiredService<ISchemaValidatorFactory>();
        var contextFactory = provider.GetRequiredService<IJsonValidationContextFactory>();

        var schemaJson = """{"type": "string", "x-display-order": 5}""";
        using var schemaDoc = JsonDocument.Parse(schemaJson);
        repo.TryRegisterSchema(schemaDoc.RootElement, out var schemaData);
        var validator = validatorFactory.GetValidator(schemaData!.SchemaUri!);

        using var instanceDoc = JsonDocument.Parse("\"hello\"");
        var context = contextFactory.CreateContextForRoot(instanceDoc.RootElement);
        var result = validator.ValidateWithOutput(context, OutputFormat.Detailed);

        Assert.True(result.Valid);
        var annotation = FindAnnotation(result, "x-display-order");
        Assert.NotNull(annotation);
        var value = GetAnnotationJsonElement(annotation, "x-display-order");
        Assert.Equal(5, value.GetInt32());
    }

    #endregion

    #region Boolean schemas — factory returns null

    [Fact]
    public void BooleanSchema_DoesNotEmitAnnotations()
    {
        // Boolean schema true — no keyword properties to extract
        var result = JsonSchemaValidator.Validate("true", "\"hello\"", DefaultOptions, OutputFormat.Detailed);

        Assert.True(result.Valid);
        // No annotations expected from a boolean schema
        Assert.Null(result.Annotations);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Finds an annotation OutputUnit for the given keyword in the Detailed output tree.
    /// </summary>
    private static OutputUnit? FindAnnotation(OutputUnit output, string keyword)
    {
        // Check direct annotations list
        if (output.Annotations != null)
        {
            foreach (var ann in output.Annotations)
            {
                if (ann.Annotation is IReadOnlyDictionary<string, object?> dict && dict.ContainsKey(keyword))
                {
                    return ann;
                }

                // Recurse into nested annotations
                var nested = FindAnnotation(ann, keyword);
                if (nested != null)
                {
                    return nested;
                }
            }
        }

        // Check if this output unit itself has the annotation
        if (output.Annotation is IReadOnlyDictionary<string, object?> selfDict && selfDict.ContainsKey(keyword))
        {
            return output;
        }

        return null;
    }

    /// <summary>
    /// Extracts a typed annotation value from an OutputUnit's Annotation dictionary.
    /// </summary>
    private static T? GetAnnotationValue<T>(OutputUnit annotation, string keyword)
    {
        var dict = (IReadOnlyDictionary<string, object?>)annotation.Annotation!;
        return (T?)dict[keyword];
    }

    /// <summary>
    /// Extracts a JsonElement annotation value from an OutputUnit's Annotation dictionary.
    /// </summary>
    private static JsonElement GetAnnotationJsonElement(OutputUnit annotation, string keyword)
    {
        var dict = (IReadOnlyDictionary<string, object?>)annotation.Annotation!;
        return (JsonElement)dict[keyword]!;
    }

    #endregion
}
