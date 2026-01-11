using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Validation;
using JsonSchemaValidation.Validation.Output;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidationTests.Draft202012.OutputFormat;

/// <summary>
/// Comprehensive regression tests for JSON Schema 2020-12 output formats.
/// Tests cover edge cases, RFC 6901 compliance, and all annotation-producing keywords.
/// </summary>
[Trait("Draft", "2020-12")]
public class RegressionTests
{
    private readonly ISchemaRepository _schemaRepository;
    private readonly ISchemaValidatorFactory _schemaValidatorFactory;
    private readonly IJsonValidationContextFactory _jsonValidationContextFactory;

    public RegressionTests()
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt => opt.EnableDraft202012 = true);
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.InitializeSingletonServices();

        _schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        _schemaValidatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        _jsonValidationContextFactory = serviceProvider.GetRequiredService<IJsonValidationContextFactory>();
    }

    #region Flag Output Tests

    [Fact]
    public void FlagOutput_ValidInstance_ReturnsTrue()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("\"test\"").RootElement;

        var output = ValidateFlag(schema, instance);

        Assert.True(output.Valid);
    }

    [Fact]
    public void FlagOutput_InvalidInstance_ReturnsFalse()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var output = ValidateFlag(schema, instance);

        Assert.False(output.Valid);
    }

    [Fact]
    public void FlagOutput_DoesNotLeakErrorDetails()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var output = ValidateFlag(schema, instance);

        Assert.Null(output.Errors);
        Assert.Null(output.Error);
        Assert.Null(output.Annotations);
    }

    [Fact]
    public void FlagOutput_EmptyLocationsForEfficiency()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var output = ValidateFlag(schema, instance);

        Assert.Equal("", output.InstanceLocation);
        Assert.Equal("", output.KeywordLocation);
    }

    #endregion

    #region Basic Output Tests

    [Fact]
    public void BasicOutput_ValidInstance_HasEmptyErrorsList()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("\"test\"").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.True(output.Valid);
        Assert.Null(output.Errors);
    }

    [Fact]
    public void BasicOutput_CollectsErrorsFromNestedStructure()
    {
        var schema = JsonDocument.Parse("""
            {
                "properties": {
                    "a": {"properties": {"b": {"type": "number"}}}
                }
            }
            """).RootElement;
        var instance = JsonDocument.Parse("""{"a": {"b": "not a number"}}""").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
        Assert.Contains(output.Errors, e => e.InstanceLocation == "/a/b");
    }

    [Fact]
    public void BasicOutput_FlattenedErrors_NoNestedErrorsProperty()
    {
        var schema = JsonDocument.Parse("""
            {"allOf": [{"type": "object"}, {"required": ["a", "b"]}]}
            """).RootElement;
        var instance = JsonDocument.Parse("{}").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
        // Basic output errors should not have nested Errors
        Assert.All(output.Errors, e => Assert.Null(e.Errors));
    }

    [Fact]
    public void BasicOutput_ErrorCountInSummaryMessage()
    {
        var schema = JsonDocument.Parse("""{"required": ["a", "b", "c"]}""").RootElement;
        var instance = JsonDocument.Parse("{}").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Error);
        Assert.Contains("error", output.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BasicOutput_SingleError_SingularMessage()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
        Assert.Single(output.Errors);
    }

    #endregion

    #region Detailed Output Tests

    [Fact]
    public void DetailedOutput_PreservesHierarchicalNesting()
    {
        var schema = JsonDocument.Parse("""
            {
                "allOf": [
                    {"required": ["name"]},
                    {"required": ["age"]}
                ]
            }
            """).RootElement;
        var instance = JsonDocument.Parse("{}").RootElement;

        var output = ValidateDetailed(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
    }

    [Fact]
    public void DetailedOutput_AnyOf_ShowsAllBranchFailures()
    {
        var schema = JsonDocument.Parse("""
            {
                "anyOf": [
                    {"type": "string"},
                    {"type": "number"}
                ]
            }
            """).RootElement;
        var instance = JsonDocument.Parse("true").RootElement;

        var output = ValidateDetailed(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
    }

    [Fact]
    public void DetailedOutput_OneOf_ShowsMultipleMatchFailure()
    {
        var schema = JsonDocument.Parse("""
            {
                "oneOf": [
                    {"type": "number"},
                    {"minimum": 0}
                ]
            }
            """).RootElement;
        var instance = JsonDocument.Parse("5").RootElement;

        var output = ValidateDetailed(schema, instance);

        // Both match, so oneOf fails
        Assert.False(output.Valid);
    }

    [Fact]
    public void DetailedOutput_IncludesAnnotationsFromValidBranches()
    {
        var schema = JsonDocument.Parse("""
            {"properties": {"name": {"type": "string"}}}
            """).RootElement;
        var instance = JsonDocument.Parse("""{"name": "John"}""").RootElement;

        var output = ValidateDetailed(schema, instance);

        Assert.True(output.Valid);
        Assert.NotNull(output.Annotations);
    }

    #endregion

    #region Instance Location - RFC 6901 Compliance

    [Fact]
    public void InstanceLocation_RootLevel_IsEmptyString()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.InstanceLocation == "");
    }

    [Fact]
    public void InstanceLocation_ObjectProperty_HasSlashPrefix()
    {
        var schema = JsonDocument.Parse("""{"properties": {"foo": {"type": "number"}}}""").RootElement;
        var instance = JsonDocument.Parse("""{"foo": "bar"}""").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/foo");
    }

    [Fact]
    public void InstanceLocation_ArrayIndex_UsesNumericPath()
    {
        var schema = JsonDocument.Parse("""{"items": {"type": "number"}}""").RootElement;
        var instance = JsonDocument.Parse("""[1, "two", 3]""").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/1");
    }

    [Fact]
    public void InstanceLocation_DeepNesting_FullPath()
    {
        var schema = JsonDocument.Parse("""
            {
                "properties": {
                    "level1": {
                        "properties": {
                            "level2": {
                                "items": {
                                    "properties": {
                                        "value": {"type": "number"}
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """).RootElement;
        var instance = JsonDocument.Parse("""
            {"level1": {"level2": [{"value": "not a number"}]}}
            """).RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/level1/level2/0/value");
    }

    [Fact]
    public void InstanceLocation_TildeEscaping_RFC6901()
    {
        var schema = JsonDocument.Parse("""{"properties": {"a~b": {"type": "number"}}}""").RootElement;
        var instance = JsonDocument.Parse("""{"a~b": "string"}""").RootElement;

        var output = ValidateBasic(schema, instance);

        // Per RFC 6901: ~ becomes ~0
        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/a~0b");
    }

    [Fact]
    public void InstanceLocation_SlashEscaping_RFC6901()
    {
        var schema = JsonDocument.Parse("""{"properties": {"a/b": {"type": "number"}}}""").RootElement;
        var instance = JsonDocument.Parse("""{"a/b": "string"}""").RootElement;

        var output = ValidateBasic(schema, instance);

        // Per RFC 6901: / becomes ~1
        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/a~1b");
    }

    [Fact]
    public void InstanceLocation_CombinedEscaping_TildeAndSlash()
    {
        var schema = JsonDocument.Parse("""{"properties": {"a~/b": {"type": "number"}}}""").RootElement;
        var instance = JsonDocument.Parse("""{"a~/b": "string"}""").RootElement;

        var output = ValidateBasic(schema, instance);

        // ~ becomes ~0, / becomes ~1
        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/a~0~1b");
    }

    [Fact]
    public void InstanceLocation_UnicodePropertyName()
    {
        var schema = JsonDocument.Parse("""{"properties": {"日本語": {"type": "number"}}}""").RootElement;
        var instance = JsonDocument.Parse("""{"日本語": "string"}""").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/日本語");
    }

    [Fact]
    public void InstanceLocation_EmptyStringPropertyName()
    {
        var schema = JsonDocument.Parse("""{"properties": {"": {"type": "number"}}}""").RootElement;
        var instance = JsonDocument.Parse("""{"": "string"}""").RootElement;

        var output = ValidateBasic(schema, instance);

        // Empty property name: /
        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/");
    }

    [Fact]
    public void InstanceLocation_NumericStringPropertyName_NotArrayIndex()
    {
        var schema = JsonDocument.Parse("""{"properties": {"123": {"type": "number"}}}""").RootElement;
        var instance = JsonDocument.Parse("""{"123": "string"}""").RootElement;

        var output = ValidateBasic(schema, instance);

        // Property "123" is a string key, not an array index
        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/123");
    }

    [Fact]
    public void InstanceLocation_LargeArrayIndex()
    {
        var schema = JsonDocument.Parse("""{"prefixItems": [{}, {}, {}, {}, {}, {}, {}, {}, {}, {}, {"type": "number"}]}""").RootElement;
        var items = string.Join(", ", Enumerable.Repeat("1", 10)) + ", \"string\"";
        var instance = JsonDocument.Parse($"[{items}]").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/10");
    }

    #endregion

    #region Keyword Location Tests

    [Fact]
    public void KeywordLocation_SimpleKeyword()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.KeywordLocation == "/type");
    }

    [Fact]
    public void KeywordLocation_NestedProperties()
    {
        var schema = JsonDocument.Parse("""
            {"properties": {"user": {"properties": {"age": {"minimum": 0}}}}}
            """).RootElement;
        var instance = JsonDocument.Parse("""{"user": {"age": -1}}""").RootElement;

        var output = ValidateBasic(schema, instance);

        var error = output.Errors!.First(e => e.KeywordLocation.Contains("minimum"));
        Assert.Contains("/properties/user/properties/age/minimum", error.KeywordLocation);
    }

    [Fact]
    public void KeywordLocation_AllOfPath()
    {
        var schema = JsonDocument.Parse("""
            {"allOf": [{"type": "object"}, {"required": ["name"]}]}
            """).RootElement;
        var instance = JsonDocument.Parse("{}").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.KeywordLocation.Contains("/allOf/"));
    }

    [Fact]
    public void KeywordLocation_AnyOfPath()
    {
        var schema = JsonDocument.Parse("""
            {"anyOf": [{"type": "string"}, {"type": "number"}]}
            """).RootElement;
        var instance = JsonDocument.Parse("true").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.KeywordLocation.Contains("/anyOf/"));
    }

    [Fact]
    public void KeywordLocation_OneOfPath()
    {
        var schema = JsonDocument.Parse("""
            {"oneOf": [{"type": "boolean"}, {"const": "test"}]}
            """).RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.KeywordLocation.Contains("/oneOf"));
    }

    [Fact]
    public void KeywordLocation_ItemsPath()
    {
        var schema = JsonDocument.Parse("""{"items": {"type": "number"}}""").RootElement;
        var instance = JsonDocument.Parse("""["string"]""").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.KeywordLocation.Contains("/items"));
    }

    [Fact]
    public void KeywordLocation_PrefixItemsPath()
    {
        var schema = JsonDocument.Parse("""{"prefixItems": [{"type": "string"}, {"type": "number"}]}""").RootElement;
        var instance = JsonDocument.Parse("""[123, "string"]""").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.Contains(output.Errors!, e => e.KeywordLocation.Contains("/prefixItems/"));
    }

    #endregion

    #region Annotation Tests - Properties

    [Fact]
    public void Annotations_Properties_ListsEvaluatedProperties()
    {
        var schema = JsonDocument.Parse("""
            {"properties": {"a": {}, "b": {}, "c": {}}}
            """).RootElement;
        var instance = JsonDocument.Parse("""{"a": 1, "c": 3}""").RootElement;

        var result = ValidateRoot(schema, instance);

        var annotation = GetAnnotation<List<string>>(result, "properties");
        Assert.NotNull(annotation);
        Assert.Contains("a", annotation);
        Assert.Contains("c", annotation);
        Assert.DoesNotContain("b", annotation);
    }

    [Fact]
    public void Annotations_PatternProperties_ListsMatchedProperties()
    {
        var schema = JsonDocument.Parse("""
            {"patternProperties": {"^x-": {"type": "string"}}}
            """).RootElement;
        var instance = JsonDocument.Parse("""{"x-custom": "value", "other": 123}""").RootElement;

        var result = ValidateRoot(schema, instance);

        var annotation = GetAnnotation<List<string>>(result, "patternProperties");
        Assert.NotNull(annotation);
        Assert.Contains("x-custom", annotation);
        Assert.DoesNotContain("other", annotation);
    }

    [Fact]
    public void Annotations_AdditionalProperties_ListsAdditionalProperties()
    {
        var schema = JsonDocument.Parse("""
            {
                "properties": {"known": {}},
                "additionalProperties": {"type": "number"}
            }
            """).RootElement;
        var instance = JsonDocument.Parse("""{"known": "x", "extra1": 1, "extra2": 2}""").RootElement;

        var result = ValidateRoot(schema, instance);

        var annotation = GetAnnotation<List<string>>(result, "additionalProperties");
        Assert.NotNull(annotation);
        Assert.Contains("extra1", annotation);
        Assert.Contains("extra2", annotation);
        Assert.DoesNotContain("known", annotation);
    }

    #endregion

    #region Annotation Tests - Arrays

    [Fact]
    public void Annotations_PrefixItems_ReturnsLargestIndex()
    {
        var schema = JsonDocument.Parse("""
            {"prefixItems": [{"type": "string"}, {"type": "number"}, {"type": "boolean"}]}
            """).RootElement;
        var instance = JsonDocument.Parse("""["a", 1, true, "extra"]""").RootElement;

        var result = ValidateRoot(schema, instance);

        var annotation = GetAnnotation<int>(result, "prefixItems");
        Assert.Equal(2, annotation); // 0-based index, so 2 means indices 0, 1, 2
    }

    [Fact]
    public void Annotations_Items_ReturnsTrueWhenApplied()
    {
        var schema = JsonDocument.Parse("""
            {"prefixItems": [{}], "items": {"type": "number"}}
            """).RootElement;
        var instance = JsonDocument.Parse("""["first", 1, 2, 3]""").RootElement;

        var result = ValidateRoot(schema, instance);

        var annotation = GetAnnotation<bool>(result, "items");
        Assert.True(annotation);
    }

    [Fact]
    public void Annotations_Contains_ListsMatchingIndices()
    {
        var schema = JsonDocument.Parse("""{"contains": {"type": "string"}}""").RootElement;
        var instance = JsonDocument.Parse("""[1, "a", 2, "b", 3]""").RootElement;

        var result = ValidateRoot(schema, instance);

        var annotation = GetAnnotation<List<int>>(result, "contains");
        Assert.NotNull(annotation);
        Assert.Contains(1, annotation);
        Assert.Contains(3, annotation);
        Assert.DoesNotContain(0, annotation);
        Assert.DoesNotContain(2, annotation);
        Assert.DoesNotContain(4, annotation);
    }

    [Fact]
    public void Annotations_UnevaluatedItems_ListsValidatedIndices()
    {
        var schema = JsonDocument.Parse("""
            {
                "prefixItems": [{"type": "string"}],
                "unevaluatedItems": {"type": "number"}
            }
            """).RootElement;
        var instance = JsonDocument.Parse("""["first", 1, 2]""").RootElement;

        var result = ValidateRoot(schema, instance);

        var annotation = GetAnnotation<List<int>>(result, "unevaluatedItems");
        Assert.NotNull(annotation);
        Assert.Contains(1, annotation);
        Assert.Contains(2, annotation);
    }

    [Fact]
    public void Annotations_UnevaluatedProperties_ListsValidatedProperties()
    {
        var schema = JsonDocument.Parse("""
            {
                "properties": {"known": {}},
                "unevaluatedProperties": {"type": "string"}
            }
            """).RootElement;
        var instance = JsonDocument.Parse("""{"known": 1, "unknown": "value"}""").RootElement;

        var result = ValidateRoot(schema, instance);

        var annotation = GetAnnotation<List<string>>(result, "unevaluatedProperties");
        Assert.NotNull(annotation);
        Assert.Contains("unknown", annotation);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EdgeCase_EmptySchema_AlwaysValid()
    {
        var schema = JsonDocument.Parse("{}").RootElement;

        Assert.True(ValidateFlag(schema, JsonDocument.Parse("null").RootElement).Valid);
        Assert.True(ValidateFlag(schema, JsonDocument.Parse("123").RootElement).Valid);
        Assert.True(ValidateFlag(schema, JsonDocument.Parse("\"string\"").RootElement).Valid);
        Assert.True(ValidateFlag(schema, JsonDocument.Parse("[]").RootElement).Valid);
        Assert.True(ValidateFlag(schema, JsonDocument.Parse("{}").RootElement).Valid);
    }

    [Fact]
    public void EdgeCase_BooleanSchemaTrue_AlwaysValid()
    {
        var schema = JsonDocument.Parse("true").RootElement;

        Assert.True(ValidateFlag(schema, JsonDocument.Parse("null").RootElement).Valid);
        Assert.True(ValidateFlag(schema, JsonDocument.Parse("123").RootElement).Valid);
        Assert.True(ValidateFlag(schema, JsonDocument.Parse("{}").RootElement).Valid);
    }

    [Fact]
    public void EdgeCase_BooleanSchemaFalse_AlwaysInvalid()
    {
        var schema = JsonDocument.Parse("false").RootElement;

        Assert.False(ValidateFlag(schema, JsonDocument.Parse("null").RootElement).Valid);
        Assert.False(ValidateFlag(schema, JsonDocument.Parse("123").RootElement).Valid);
        Assert.False(ValidateFlag(schema, JsonDocument.Parse("{}").RootElement).Valid);
    }

    [Fact]
    public void EdgeCase_MultipleErrorsAtSameLocation()
    {
        var schema = JsonDocument.Parse("""
            {"type": "string", "minLength": 5, "pattern": "^[a-z]+$"}
            """).RootElement;
        // "AB" fails: not minLength 5, and pattern requires lowercase
        var instance = JsonDocument.Parse("\"AB\"").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
        // Multiple errors at the same instance location ""
        var rootErrors = output.Errors.Where(e => e.InstanceLocation == "").ToList();
        Assert.True(rootErrors.Count >= 2);
    }

    [Fact]
    public void EdgeCase_VeryDeepNesting()
    {
        // Build a schema with 15 levels of nesting
        var schemaJson = """{"properties": {"l1": {"properties": {"l2": {"properties": {"l3": {"properties": {"l4": {"properties": {"l5": {"properties": {"l6": {"properties": {"l7": {"properties": {"l8": {"properties": {"l9": {"properties": {"l10": {"type": "number"}}}}}}}}}}}}}}}}}}}}}""";
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        var instanceJson = """{"l1": {"l2": {"l3": {"l4": {"l5": {"l6": {"l7": {"l8": {"l9": {"l10": "string"}}}}}}}}}}""";
        var instance = JsonDocument.Parse(instanceJson).RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/l1/l2/l3/l4/l5/l6/l7/l8/l9/l10");
    }

    [Fact]
    public void EdgeCase_LargeArray_100Items()
    {
        var schema = JsonDocument.Parse("""{"items": {"type": "number"}}""").RootElement;

        var items = string.Join(", ", Enumerable.Range(0, 99).Select(i => i.ToString())) + ", \"string\"";
        var instance = JsonDocument.Parse($"[{items}]").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.Contains(output.Errors!, e => e.InstanceLocation == "/99");
    }

    [Fact]
    public void EdgeCase_AllAnnotationProducingKeywords()
    {
        var schema = JsonDocument.Parse("""
            {
                "properties": {"a": {}},
                "patternProperties": {"^x-": {}},
                "additionalProperties": {},
                "prefixItems": [{}],
                "items": {},
                "contains": {"type": "string"}
            }
            """).RootElement;
        var instance = JsonDocument.Parse("""
            {
                "a": 1,
                "x-custom": 2,
                "other": 3
            }
            """).RootElement;

        // This should produce annotations for properties, patternProperties, and additionalProperties
        var result = ValidateRoot(schema, instance);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Children);

        // Verify we have annotation-producing results
        var hasPropertiesAnnotation = result.Children.Any(c => c.Annotations?.ContainsKey("properties") == true);
        var hasPatternPropertiesAnnotation = result.Children.Any(c => c.Annotations?.ContainsKey("patternProperties") == true);
        var hasAdditionalPropertiesAnnotation = result.Children.Any(c => c.Annotations?.ContainsKey("additionalProperties") == true);

        Assert.True(hasPropertiesAnnotation);
        Assert.True(hasPatternPropertiesAnnotation);
        Assert.True(hasAdditionalPropertiesAnnotation);
    }

    [Fact]
    public void EdgeCase_IfThenElse_ErrorsFromCorrectBranch()
    {
        var schema = JsonDocument.Parse("""
            {
                "if": {"properties": {"type": {"const": "A"}}},
                "then": {"required": ["valueA"]},
                "else": {"required": ["valueB"]}
            }
            """).RootElement;

        // Type is "A", so "then" applies, which requires "valueA"
        var instance = JsonDocument.Parse("""{"type": "A"}""").RootElement;
        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
        // Error should mention the missing required property
        Assert.Contains(output.Errors, e =>
            e.Error != null && e.Error.Contains("valueA", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EdgeCase_Not_ErrorMessage()
    {
        var schema = JsonDocument.Parse("""{"not": {"type": "string"}}""").RootElement;
        var instance = JsonDocument.Parse("\"test\"").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.Contains(output.Errors!, e => e.KeywordLocation.Contains("/not"));
    }

    [Fact]
    public void EdgeCase_DependentSchemas_TriggeredValidation()
    {
        var schema = JsonDocument.Parse("""
            {
                "dependentSchemas": {
                    "credit_card": {"required": ["billing_address"]}
                }
            }
            """).RootElement;
        var instance = JsonDocument.Parse("""{"credit_card": "1234-5678"}""").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
    }

    [Fact]
    public void EdgeCase_PropertyNames_InvalidName()
    {
        var schema = JsonDocument.Parse("""{"propertyNames": {"minLength": 3}}""").RootElement;
        var instance = JsonDocument.Parse("""{"ab": 1, "abc": 2}""").RootElement;

        var output = ValidateBasic(schema, instance);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
    }

    #endregion

    #region Output Format Conversion

    [Fact]
    public void ToOutputUnit_Flag_FromValidationResult()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var result = ValidateRoot(schema, instance);
        var output = result.ToOutputUnit(JsonSchemaValidation.Validation.Output.OutputFormat.Flag);

        Assert.False(output.Valid);
        Assert.Null(output.Errors);
    }

    [Fact]
    public void ToOutputUnit_Basic_FromValidationResult()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var result = ValidateRoot(schema, instance);
        var output = result.ToOutputUnit(JsonSchemaValidation.Validation.Output.OutputFormat.Basic);

        Assert.False(output.Valid);
        Assert.NotNull(output.Errors);
    }

    [Fact]
    public void ToOutputUnit_Detailed_FromValidationResult()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var instance = JsonDocument.Parse("123").RootElement;

        var result = ValidateRoot(schema, instance);
        var output = result.ToOutputUnit(JsonSchemaValidation.Validation.Output.OutputFormat.Detailed);

        Assert.False(output.Valid);
    }

    #endregion

    #region Helper Methods

    private ISchemaValidator RegisterAndGetValidator(JsonElement schema)
    {
        if (!_schemaRepository.TryRegisterSchema(schema, out var schemaData))
        {
            throw new InvalidOperationException("Schema could not be registered.");
        }
        return _schemaValidatorFactory.GetValidator(schemaData!.SchemaUri!);
    }

    private OutputUnit ValidateFlag(JsonElement schema, JsonElement instance)
    {
        var validator = RegisterAndGetValidator(schema);
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        return validator.ValidateFlag(context);
    }

    private OutputUnit ValidateBasic(JsonElement schema, JsonElement instance)
    {
        var validator = RegisterAndGetValidator(schema);
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        return validator.ValidateBasic(context);
    }

    private OutputUnit ValidateDetailed(JsonElement schema, JsonElement instance)
    {
        var validator = RegisterAndGetValidator(schema);
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        return validator.ValidateDetailed(context);
    }

    private ValidationResult ValidateRoot(JsonElement schema, JsonElement instance)
    {
        var validator = RegisterAndGetValidator(schema);
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        return validator.ValidateRoot(context);
    }

    private T? GetAnnotation<T>(ValidationResult result, string keyword)
    {
        if (result.Annotations?.TryGetValue(keyword, out var value) == true && value is T typedValue)
        {
            return typedValue;
        }

        if (result.Children != null)
        {
            foreach (var child in result.Children)
            {
                var found = GetAnnotation<T>(child, keyword);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return default;
    }

    #endregion
}
