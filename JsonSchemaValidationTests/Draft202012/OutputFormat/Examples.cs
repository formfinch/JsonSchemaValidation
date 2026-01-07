using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Validation.Output;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidationTests.Draft202012.OutputFormat;

/// <summary>
/// Example tests demonstrating how to use JSON Schema 2020-12 output formats.
/// These tests serve as documentation for library consumers.
///
/// JSON Schema 2020-12 Section 12 defines three output formats:
/// - Flag: Simple boolean result (most efficient)
/// - Basic: Flat list of all errors with locations
/// - Detailed: Hierarchical structure matching schema nesting
/// </summary>
public class Examples
{
    private readonly ISchemaRepository _schemaRepository;
    private readonly ISchemaValidatorFactory _schemaValidatorFactory;
    private readonly IJsonValidationContextFactory _jsonValidationContextFactory;

    public Examples()
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt => opt.EnableDraft202012 = true);
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.InitializeSingletonServices();

        _schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        _schemaValidatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        _jsonValidationContextFactory = serviceProvider.GetRequiredService<IJsonValidationContextFactory>();
    }

    #region Flag Output Examples

    /// <summary>
    /// Flag output is the most efficient format when you only need to know
    /// if validation passed or failed, without any error details.
    ///
    /// Use case: High-performance validation where you'll reject invalid
    /// data without needing to explain why.
    /// </summary>
    [Fact]
    public void Example_FlagOutput_QuickPassFailCheck()
    {
        // Schema: expects a string value
        var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Valid case: string matches the type
        var validInstance = JsonDocument.Parse("\"hello world\"").RootElement;
        var validContext = _jsonValidationContextFactory.CreateContextForRoot(validInstance);
        var validResult = validator.ValidateFlag(validContext);

        Assert.True(validResult.Valid);

        // Invalid case: number does not match string type
        var invalidInstance = JsonDocument.Parse("42").RootElement;
        var invalidContext = _jsonValidationContextFactory.CreateContextForRoot(invalidInstance);
        var invalidResult = validator.ValidateFlag(invalidContext);

        Assert.False(invalidResult.Valid);
        // Note: Flag output provides no error details - Errors is null
        Assert.Null(invalidResult.Errors);
    }

    #endregion

    #region Basic Output Examples

    /// <summary>
    /// Basic output provides a flat list of all validation errors.
    /// Each error includes the location in the JSON instance where it occurred.
    ///
    /// Use case: API responses where you need to return all validation errors
    /// in a simple, easy-to-process format.
    /// </summary>
    [Fact]
    public void Example_BasicOutput_FlatErrorList()
    {
        // Schema: object with required name (string) and age (integer >= 0)
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": {"type": "string"},
                    "age": {"type": "integer", "minimum": 0}
                },
                "required": ["name", "age"]
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Invalid instance: age is wrong type and negative value wouldn't matter
        var instance = JsonDocument.Parse("""{"name": "John", "age": "twenty-five"}""").RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        var result = validator.ValidateBasic(context);

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        // Basic output flattens all errors into a single list
        // Each error has InstanceLocation pointing to where in the JSON the error occurred
        var ageError = result.Errors.FirstOrDefault(e => e.InstanceLocation == "/age");
        Assert.NotNull(ageError);
        Assert.Contains("integer", ageError.Error!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Instance locations use JSON Pointer format (RFC 6901) to identify
    /// the exact location in the JSON document where validation failed.
    ///
    /// Use case: Highlighting specific fields in a form that failed validation.
    /// </summary>
    [Fact]
    public void Example_BasicOutput_UsingInstanceLocationToFindErrors()
    {
        // Schema: validates an array of user objects
        var schema = JsonDocument.Parse("""
            {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "email": {"type": "string", "minLength": 5}
                    },
                    "required": ["email"]
                }
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Invalid: second user has email that's too short
        var instance = JsonDocument.Parse("""
            [
                {"email": "alice@example.com"},
                {"email": "bob"}
            ]
            """).RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        var result = validator.ValidateBasic(context);

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        // The error location points to /1/email (array index 1, property "email")
        var emailError = result.Errors.FirstOrDefault(e => e.InstanceLocation == "/1/email");
        Assert.NotNull(emailError);

        // You can parse this location to highlight the specific field in your UI
        // "/1/email" means: root array -> index 1 -> property "email"
    }

    /// <summary>
    /// Keyword location shows which schema keyword caused the validation failure.
    /// This is useful for debugging complex schemas.
    ///
    /// Use case: Schema developers debugging why validation failed.
    /// </summary>
    [Fact]
    public void Example_BasicOutput_KeywordLocationForDebugging()
    {
        // Schema: nested properties with a minimum constraint
        var schema = JsonDocument.Parse("""
            {
                "properties": {
                    "settings": {
                        "properties": {
                            "timeout": {"minimum": 1}
                        }
                    }
                }
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Invalid: timeout is less than minimum
        var instance = JsonDocument.Parse("""{"settings": {"timeout": 0}}""").RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        var result = validator.ValidateBasic(context);

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        // KeywordLocation shows the path through the schema to the failing keyword
        var error = result.Errors.First(e => e.KeywordLocation.Contains("minimum"));
        Assert.Contains("/properties/settings/properties/timeout/minimum", error.KeywordLocation);
    }

    #endregion

    #region Detailed Output Examples

    /// <summary>
    /// Detailed output preserves the hierarchical structure of validation,
    /// matching how the schema is nested. This is most useful for complex
    /// schemas with allOf, anyOf, oneOf constructs.
    ///
    /// Use case: Rich error reporting that shows the full validation tree.
    /// </summary>
    [Fact]
    public void Example_DetailedOutput_HierarchicalErrorStructure()
    {
        // Schema: allOf with multiple constraints
        var schema = JsonDocument.Parse("""
            {
                "allOf": [
                    {"type": "object"},
                    {"required": ["name"]},
                    {"required": ["email"]}
                ]
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Invalid: empty object missing both required properties
        var instance = JsonDocument.Parse("{}").RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        var result = validator.ValidateDetailed(context);

        Assert.False(result.Valid);
        Assert.NotNull(result.Errors);

        // Detailed output nests errors under their parent validators
        // The allOf has child errors for each failed subschema
        Assert.True(result.Errors.Count >= 1);
    }

    /// <summary>
    /// When validation succeeds, detailed output can include annotations.
    /// Annotations are metadata produced by successful validation keywords.
    ///
    /// Use case: Understanding which parts of the schema were applied to the data.
    /// </summary>
    [Fact]
    public void Example_DetailedOutput_AnnotationsOnSuccess()
    {
        // Schema: properties with additionalProperties
        var schema = JsonDocument.Parse("""
            {
                "properties": {
                    "name": {"type": "string"}
                },
                "additionalProperties": {"type": "number"}
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Valid instance with known and additional properties
        var instance = JsonDocument.Parse("""{"name": "John", "score": 100}""").RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        var result = validator.ValidateDetailed(context);

        Assert.True(result.Valid);
        // Detailed output includes annotations showing which keywords evaluated what
        Assert.NotNull(result.Annotations);
    }

    #endregion

    #region Annotation Examples

    /// <summary>
    /// The 'properties' keyword produces an annotation listing which
    /// properties from the instance were validated against the schema.
    ///
    /// Use case: Determining which properties were recognized by the schema.
    /// </summary>
    [Fact]
    public void Example_Annotations_PropertiesKeyword()
    {
        var schema = JsonDocument.Parse("""
            {
                "properties": {
                    "firstName": {"type": "string"},
                    "lastName": {"type": "string"},
                    "age": {"type": "integer"}
                }
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Instance has two of the three defined properties
        var instance = JsonDocument.Parse("""{"firstName": "John", "age": 30}""").RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        var result = validator.ValidateRoot(context);

        Assert.True(result.IsValid);

        // Find the annotation from the properties keyword
        var propertiesResult = result.Children?.FirstOrDefault(
            c => c.Annotations?.ContainsKey("properties") == true);
        Assert.NotNull(propertiesResult);

        var evaluatedProperties = propertiesResult.Annotations!["properties"] as List<string>;
        Assert.NotNull(evaluatedProperties);
        Assert.Contains("firstName", evaluatedProperties);
        Assert.Contains("age", evaluatedProperties);
        // lastName is not in the annotation because it wasn't in the instance
        Assert.DoesNotContain("lastName", evaluatedProperties);
    }

    /// <summary>
    /// The 'contains' keyword produces an annotation listing the array
    /// indices that matched the contains schema.
    ///
    /// Use case: Finding which array items matched a specific criteria.
    /// </summary>
    [Fact]
    public void Example_Annotations_ContainsKeyword()
    {
        var schema = JsonDocument.Parse("""
            {
                "type": "array",
                "contains": {"type": "string", "minLength": 5}
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Array with some strings meeting the length requirement
        var instance = JsonDocument.Parse("""[1, "hi", "hello", 2, "world"]""").RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        var result = validator.ValidateRoot(context);

        Assert.True(result.IsValid);

        // Find which indices matched the contains schema
        var containsResult = result.Children?.FirstOrDefault(
            c => c.Annotations?.ContainsKey("contains") == true);
        Assert.NotNull(containsResult);

        var matchingIndices = containsResult.Annotations!["contains"] as List<int>;
        Assert.NotNull(matchingIndices);
        // Indices 2 ("hello") and 4 ("world") have length >= 5
        Assert.Contains(2, matchingIndices);
        Assert.Contains(4, matchingIndices);
    }

    /// <summary>
    /// The 'prefixItems' keyword produces an annotation indicating the
    /// largest index that was validated against the prefix schemas.
    ///
    /// Use case: Knowing how many array items were validated by prefixItems
    /// vs. the 'items' keyword.
    /// </summary>
    [Fact]
    public void Example_Annotations_PrefixItemsAndItems()
    {
        var schema = JsonDocument.Parse("""
            {
                "prefixItems": [
                    {"type": "string"},
                    {"type": "number"}
                ],
                "items": {"type": "boolean"}
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Array: first two items match prefixItems, rest must be booleans
        var instance = JsonDocument.Parse("""["name", 42, true, false]""").RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        var result = validator.ValidateRoot(context);

        Assert.True(result.IsValid);

        // prefixItems annotation: largest index validated (0-based, so 1 means indices 0 and 1)
        var prefixResult = result.Children?.FirstOrDefault(
            c => c.Annotations?.ContainsKey("prefixItems") == true);
        Assert.NotNull(prefixResult);
        Assert.Equal(1, prefixResult.Annotations!["prefixItems"]);

        // items annotation: true means items keyword was applied to remaining elements
        var itemsResult = result.Children?.FirstOrDefault(
            c => c.Annotations?.ContainsKey("items") == true);
        Assert.NotNull(itemsResult);
        Assert.Equal(true, itemsResult.Annotations!["items"]);
    }

    /// <summary>
    /// The 'additionalProperties' keyword produces an annotation listing
    /// properties that were validated as additional (not defined in 'properties'
    /// or matched by 'patternProperties').
    ///
    /// Use case: Identifying unknown/extra fields in user input.
    /// </summary>
    [Fact]
    public void Example_Annotations_AdditionalProperties()
    {
        var schema = JsonDocument.Parse("""
            {
                "properties": {
                    "id": {"type": "integer"}
                },
                "additionalProperties": {"type": "string"}
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        // Instance has one known property and two additional ones
        var instance = JsonDocument.Parse("""
            {"id": 1, "note": "hello", "tag": "important"}
            """).RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
        var result = validator.ValidateRoot(context);

        Assert.True(result.IsValid);

        var additionalResult = result.Children?.FirstOrDefault(
            c => c.Annotations?.ContainsKey("additionalProperties") == true);
        Assert.NotNull(additionalResult);

        var additionalProps = additionalResult.Annotations!["additionalProperties"] as List<string>;
        Assert.NotNull(additionalProps);
        Assert.Contains("note", additionalProps);
        Assert.Contains("tag", additionalProps);
        Assert.DoesNotContain("id", additionalProps); // id is a known property
    }

    #endregion

    #region Comparing Output Formats

    /// <summary>
    /// This example shows the same validation using all three output formats,
    /// demonstrating the trade-offs between each.
    /// </summary>
    [Fact]
    public void Example_ComparingAllThreeFormats()
    {
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": {"type": "string", "minLength": 1},
                    "age": {"type": "integer", "minimum": 0}
                },
                "required": ["name"]
            }
            """).RootElement;
        var validator = RegisterAndGetValidator(schema);

        var instance = JsonDocument.Parse("""{"name": "", "age": -5}""").RootElement;
        var context = _jsonValidationContextFactory.CreateContextForRoot(instance);

        // Flag: Just tells you it failed
        var flagResult = validator.ValidateFlag(context);
        Assert.False(flagResult.Valid);
        Assert.Null(flagResult.Errors); // No details

        // Basic: Flat list of all errors
        var basicResult = validator.ValidateBasic(context);
        Assert.False(basicResult.Valid);
        Assert.NotNull(basicResult.Errors);
        Assert.True(basicResult.Errors.Count >= 2); // At least minLength and minimum errors
        // All errors are at the same level (flat)
        Assert.All(basicResult.Errors, e => Assert.Null(e.Errors));

        // Detailed: Hierarchical structure
        var detailedResult = validator.ValidateDetailed(context);
        Assert.False(detailedResult.Valid);
        Assert.NotNull(detailedResult.Errors);
        // Detailed can have nested errors
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

    #endregion
}
