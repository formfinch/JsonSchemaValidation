using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Validation.Output;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace JsonSchemaValidationTests.Draft202012
{
    /// <summary>
    /// Tests for JSON Schema 2020-12 Section 12 output formats (Flag, Basic, Detailed).
    /// </summary>
    public class OutputFormatTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISchemaRepository _schemaRepository;
        private readonly ISchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _jsonValidationContextFactory;

        public OutputFormatTests()
        {
            var services = new ServiceCollection();
            services.AddJsonSchemaValidation(opt =>
            {
                opt.EnableDraft202012 = true;
            });
            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.InitializeSingletonServices();

            _schemaRepository = _serviceProvider.GetRequiredService<ISchemaRepository>();
            _schemaValidatorFactory = _serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
            _jsonValidationContextFactory = _serviceProvider.GetRequiredService<IJsonValidationContextFactory>();
        }

        #region Flag Output Tests

        [Fact]
        public void FlagOutput_ValidInstance_ReturnsValidTrue()
        {
            var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
            var instance = JsonDocument.Parse("\"test\"").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateFlag(context);

            Assert.True(output.Valid);
        }

        [Fact]
        public void FlagOutput_InvalidInstance_ReturnsValidFalse()
        {
            var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
            var instance = JsonDocument.Parse("123").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateFlag(context);

            Assert.False(output.Valid);
        }

        [Fact]
        public void FlagOutput_NoErrorDetails()
        {
            var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
            var instance = JsonDocument.Parse("123").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateFlag(context);

            Assert.Null(output.Errors);
            Assert.Null(output.Error);
        }

        #endregion

        #region Basic Output Tests

        [Fact]
        public void BasicOutput_ValidInstance_ReturnsValidTrue()
        {
            var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
            var instance = JsonDocument.Parse("\"test\"").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.True(output.Valid);
            Assert.Equal("", output.InstanceLocation);
            Assert.Equal("", output.KeywordLocation);
        }

        [Fact]
        public void BasicOutput_InvalidInstance_ContainsErrorsFlat()
        {
            var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
            var instance = JsonDocument.Parse("123").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            Assert.NotEmpty(output.Errors);
            Assert.All(output.Errors, e => Assert.NotNull(e.Error));
        }

        [Fact]
        public void BasicOutput_NestedError_HasCorrectInstanceLocation()
        {
            var schema = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "name": {"type": "string"},
                        "age": {"type": "integer"}
                    }
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""{"name": "John", "age": "thirty"}""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            var ageError = output.Errors.FirstOrDefault(e => e.InstanceLocation == "/age");
            Assert.NotNull(ageError);
            Assert.Contains("integer", ageError.Error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BasicOutput_ArrayItem_HasCorrectInstanceLocation()
        {
            var schema = JsonDocument.Parse("""
                {
                    "type": "array",
                    "items": {"type": "number"}
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""[1, 2, "three", 4]""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            var itemError = output.Errors.FirstOrDefault(e => e.InstanceLocation == "/2");
            Assert.NotNull(itemError);
        }

        [Fact]
        public void BasicOutput_DeeplyNestedError_HasFullPath()
        {
            var schema = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "users": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "email": {"type": "string", "minLength": 5}
                                }
                            }
                        }
                    }
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""{"users": [{"email": "a@b"}]}""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            var emailError = output.Errors.FirstOrDefault(e => e.InstanceLocation == "/users/0/email");
            Assert.NotNull(emailError);
        }

        #endregion

        #region Detailed Output Tests

        [Fact]
        public void DetailedOutput_ValidInstance_ReturnsValidTrue()
        {
            var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
            var instance = JsonDocument.Parse("\"test\"").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateDetailed(context);

            Assert.True(output.Valid);
        }

        [Fact]
        public void DetailedOutput_InvalidInstance_ContainsNestedErrors()
        {
            var schema = JsonDocument.Parse("""
                {
                    "allOf": [
                        {"type": "object"},
                        {"required": ["name"]},
                        {"required": ["age"]}
                    ]
                }
                """).RootElement;
            var instance = JsonDocument.Parse("{}").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateDetailed(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
        }

        [Fact]
        public void DetailedOutput_HasKeywordLocation()
        {
            var schema = JsonDocument.Parse("""{"minimum": 10}""").RootElement;
            var instance = JsonDocument.Parse("5").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateDetailed(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            var error = output.Errors.FirstOrDefault(e => e.KeywordLocation.Contains("minimum"));
            Assert.NotNull(error);
        }

        #endregion

        #region Instance Location Tests

        [Fact]
        public void InstanceLocation_RootLevel_IsEmptyString()
        {
            var schema = JsonDocument.Parse("""{"type": "number"}""").RootElement;
            var instance = JsonDocument.Parse("\"invalid\"").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            Assert.Contains(output.Errors, e => e.InstanceLocation == "");
        }

        [Fact]
        public void InstanceLocation_ObjectProperty_HasSlashPrefix()
        {
            var schema = JsonDocument.Parse("""
                {"properties": {"foo": {"type": "number"}}}
                """).RootElement;
            var instance = JsonDocument.Parse("""{"foo": "bar"}""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            Assert.Contains(output.Errors, e => e.InstanceLocation == "/foo");
        }

        [Fact]
        public void InstanceLocation_ArrayIndex_UsesNumericPath()
        {
            var schema = JsonDocument.Parse("""
                {"prefixItems": [{"type": "string"}, {"type": "number"}]}
                """).RootElement;
            var instance = JsonDocument.Parse("""["valid", "invalid"]""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            Assert.Contains(output.Errors, e => e.InstanceLocation == "/1");
        }

        [Fact]
        public void InstanceLocation_SpecialCharactersEscaped()
        {
            var schema = JsonDocument.Parse("""
                {"properties": {"a/b": {"type": "number"}, "c~d": {"type": "number"}}}
                """).RootElement;
            var instance = JsonDocument.Parse("""{"a/b": "x", "c~d": "y"}""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            // Per RFC 6901: / becomes ~1 and ~ becomes ~0
            Assert.Contains(output.Errors, e => e.InstanceLocation == "/a~1b");
            Assert.Contains(output.Errors, e => e.InstanceLocation == "/c~0d");
        }

        #endregion

        #region Keyword Location Tests

        [Fact]
        public void KeywordLocation_SimpleKeyword_HasCorrectPath()
        {
            var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
            var instance = JsonDocument.Parse("123").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            Assert.Contains(output.Errors, e => e.KeywordLocation == "/type");
        }

        [Fact]
        public void KeywordLocation_NestedProperties_HasFullPath()
        {
            var schema = JsonDocument.Parse("""
                {
                    "properties": {
                        "user": {
                            "properties": {
                                "age": {"minimum": 0}
                            }
                        }
                    }
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""{"user": {"age": -5}}""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateBasic(context);

            Assert.False(output.Valid);
            Assert.NotNull(output.Errors);
            var error = output.Errors.FirstOrDefault(e => e.KeywordLocation.Contains("minimum"));
            Assert.NotNull(error);
            Assert.Contains("/properties/user/properties/age/minimum", error.KeywordLocation);
        }

        #endregion

        #region ValidationResult Tests

        [Fact]
        public void ValidationResult_Valid_HasCorrectLocations()
        {
            var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
            var instance = JsonDocument.Parse("\"test\"").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var result = validator.ValidateRoot(context);

            Assert.True(result.IsValid);
            Assert.Equal("", result.InstanceLocation);
        }

        [Fact]
        public void ValidationResult_Invalid_HasErrorMessage()
        {
            var schema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
            var instance = JsonDocument.Parse("123").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var result = validator.ValidateRoot(context);

            Assert.False(result.IsValid);
        }

        #endregion

        #region Annotation Tests

        [Fact]
        public void Annotations_Properties_ContainsEvaluatedPropertyNames()
        {
            var schema = JsonDocument.Parse("""
                {
                    "properties": {
                        "name": {"type": "string"},
                        "age": {"type": "number"}
                    }
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""{"name": "John", "age": 30}""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var result = validator.ValidateRoot(context);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Children);
            var propertiesResult = result.Children.FirstOrDefault(c => c.Annotations?.ContainsKey("properties") == true);
            Assert.NotNull(propertiesResult);
            var annotatedProperties = propertiesResult.Annotations!["properties"] as List<string>;
            Assert.NotNull(annotatedProperties);
            Assert.Contains("name", annotatedProperties);
            Assert.Contains("age", annotatedProperties);
        }

        [Fact]
        public void Annotations_PrefixItems_ContainsLargestIndex()
        {
            var schema = JsonDocument.Parse("""
                {
                    "prefixItems": [
                        {"type": "string"},
                        {"type": "number"}
                    ]
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""["hello", 42, "extra"]""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var result = validator.ValidateRoot(context);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Children);
            var prefixItemsResult = result.Children.FirstOrDefault(c => c.Annotations?.ContainsKey("prefixItems") == true);
            Assert.NotNull(prefixItemsResult);
            // Array has 3 items but only 2 schemas, so annotation is the last validated index (1)
            Assert.Equal(1, prefixItemsResult.Annotations!["prefixItems"]);
        }

        [Fact]
        public void Annotations_Items_ReturnsTrueWhenItemsValidated()
        {
            var schema = JsonDocument.Parse("""
                {
                    "prefixItems": [{"type": "string"}],
                    "items": {"type": "number"}
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""["hello", 1, 2, 3]""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var result = validator.ValidateRoot(context);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Children);
            var itemsResult = result.Children.FirstOrDefault(c => c.Annotations?.ContainsKey("items") == true);
            Assert.NotNull(itemsResult);
            Assert.Equal(true, itemsResult.Annotations!["items"]);
        }

        [Fact]
        public void Annotations_Contains_ReturnsMatchingIndices()
        {
            var schema = JsonDocument.Parse("""
                {
                    "contains": {"type": "string"}
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""[1, "hello", 3, "world"]""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var result = validator.ValidateRoot(context);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Children);
            var containsResult = result.Children.FirstOrDefault(c => c.Annotations?.ContainsKey("contains") == true);
            Assert.NotNull(containsResult);
            var indices = containsResult.Annotations!["contains"] as List<int>;
            Assert.NotNull(indices);
            Assert.Contains(1, indices);
            Assert.Contains(3, indices);
        }

        [Fact]
        public void Annotations_AdditionalProperties_ContainsAdditionalPropertyNames()
        {
            var schema = JsonDocument.Parse("""
                {
                    "properties": {"name": {"type": "string"}},
                    "additionalProperties": {"type": "number"}
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""{"name": "John", "extra1": 1, "extra2": 2}""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var result = validator.ValidateRoot(context);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Children);
            var additionalResult = result.Children.FirstOrDefault(c => c.Annotations?.ContainsKey("additionalProperties") == true);
            Assert.NotNull(additionalResult);
            var additionalProperties = additionalResult.Annotations!["additionalProperties"] as List<string>;
            Assert.NotNull(additionalProperties);
            Assert.Contains("extra1", additionalProperties);
            Assert.Contains("extra2", additionalProperties);
        }

        [Fact]
        public void Annotations_DetailedOutput_IncludesAnnotations()
        {
            var schema = JsonDocument.Parse("""
                {
                    "properties": {"name": {"type": "string"}}
                }
                """).RootElement;
            var instance = JsonDocument.Parse("""{"name": "John"}""").RootElement;

            var validator = RegisterAndGetValidator(schema);
            var context = _jsonValidationContextFactory.CreateContextForRoot(instance);
            var output = validator.ValidateDetailed(context);

            Assert.True(output.Valid);
            // Detailed output should include annotations
            Assert.NotNull(output.Annotations);
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
}
