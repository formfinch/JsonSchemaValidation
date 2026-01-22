# TASK-004a: API Usability Evaluation

**Date:** 2026-01-22
**Status:** Complete

This document evaluates the usability of the FormFinch.JsonSchemaValidation public API before its 1.0.0 release.

---

## Executive Summary

The library now provides **two complementary APIs**:

1. **Static API** (`JsonSchemaValidator`) - Simple, zero-setup validation for common use cases
2. **DI-based API** - Full control and customization for enterprise/advanced scenarios

This dual approach offers the best of both worlds: easy adoption for new users while maintaining flexibility for advanced scenarios.

---

## Evaluation Criteria Results

### 1. Discoverability ✅ Excellent

**Finding:** The static `JsonSchemaValidator` class provides an immediately discoverable entry point.

**Simple path to first validation:**
```csharp
var result = JsonSchemaValidator.Validate(schemaJson, instanceJson);
```

Users can discover the API through:
- IntelliSense on the `JsonSchemaValidator` class
- Namespace exploration (`FormFinch.JsonSchemaValidation`)
- The single entry point guides users naturally to validation methods

### 2. Simplicity ✅ Excellent

**Minimum code to validate:**

```csharp
// 1 line - one-shot validation
var result = JsonSchemaValidator.Validate("""{"type": "string"}""", "\"hello\"");

// 2 lines - reusable compiled schema
var schema = JsonSchemaValidator.Parse("""{"type": "string"}""");
var result = schema.Validate("\"hello\"");
```

**Comparison with competing libraries:**

| Library | Lines for Simple Validation |
|---------|----------------------------|
| **FormFinch** | 1 line |
| JsonSchema.Net | 2 lines |
| NJsonSchema | 2-3 lines |
| Newtonsoft.Json.Schema | 2-3 lines |

### 3. Consistency ✅ Good

**Findings:**
- Naming conventions are consistent (`Validate`, `IsValid`, `Parse`)
- Both APIs use the same types (`OutputUnit`, `OutputFormat`, `SchemaValidationOptions`)
- Method naming follows .NET conventions
- Overloads are intuitive (string vs JsonElement, with/without options)

### 4. Documentation ✅ Good

**Findings:**
- All public APIs have comprehensive XML documentation
- Code examples in XML docs
- Self-documenting method names (`ValidateFlag`, `ValidateBasic`, `ValidateDetailed`)
- Interface `IJsonSchema` clearly documents reusable schema pattern

### 5. Flexibility ✅ Excellent

**Two API tiers for different needs:**

**Simple API** (for most users):
- `JsonSchemaValidator.Validate()` - one-shot validation
- `JsonSchemaValidator.IsValid()` - boolean check
- `JsonSchemaValidator.Parse()` - reusable schema

**Advanced API** (for enterprise/custom needs):
- Full DI integration with `AddJsonSchemaValidation()`
- Per-draft configuration options
- Format assertion control
- Pre-compiled validators for maximum performance
- Custom schema registration

### 6. Error Handling ✅ Good

**Findings:**
- Three output formats (Flag, Basic, Detailed) per JSON Schema 2020-12 spec
- Validation errors include:
  - `InstanceLocation` - JSON Pointer to the failing value
  - `KeywordLocation` - JSON Pointer to the schema keyword
  - `AbsoluteKeywordLocation` - Full URI for schema debugging
  - `Error` - Human-readable error message
- Invalid JSON throws standard `JsonException`

---

## Public API Surface

### Static API (Primary Entry Point)

```csharp
namespace FormFinch.JsonSchemaValidation;

public static class JsonSchemaValidator
{
    // One-shot validation
    OutputUnit Validate(string schema, string instance, OutputFormat format = Basic);
    OutputUnit Validate(JsonElement schema, JsonElement instance, OutputFormat format = Basic);
    OutputUnit Validate(string schema, string instance, SchemaValidationOptions options, OutputFormat format = Basic);
    OutputUnit Validate(JsonElement schema, JsonElement instance, SchemaValidationOptions options, OutputFormat format = Basic);

    // Boolean validation
    bool IsValid(string schema, string instance);
    bool IsValid(JsonElement schema, JsonElement instance);

    // Parse schema for repeated validation
    IJsonSchema Parse(string schema);
    IJsonSchema Parse(JsonElement schema);
    IJsonSchema Parse(string schema, SchemaValidationOptions options);
    IJsonSchema Parse(JsonElement schema, SchemaValidationOptions options);
}

public interface IJsonSchema
{
    Uri SchemaUri { get; }
    OutputUnit Validate(string instance, OutputFormat format = Basic);
    OutputUnit Validate(JsonElement instance, OutputFormat format = Basic);
    bool IsValid(string instance);
    bool IsValid(JsonElement instance);
}
```

### DI-based API (Advanced)

```csharp
namespace FormFinch.JsonSchemaValidation.DependencyInjection;

public static class SchemaValidationSetup
{
    IServiceCollection AddJsonSchemaValidation(this IServiceCollection services, Action<SchemaValidationOptions>? options = null);
}

public static class ServiceProviderExtensions
{
    void InitializeSingletonServices(this IServiceProvider provider);
}

public class SchemaValidationOptions
{
    string DefaultDraftVersion { get; set; }
    bool EnableDraft202012 { get; set; }  // default: true
    bool EnableDraft201909 { get; set; }  // default: true
    bool EnableDraft7 { get; set; }       // default: true
    bool EnableDraft6 { get; set; }       // default: true
    bool EnableDraft4 { get; set; }       // default: true
    bool EnableDraft3 { get; set; }       // default: true
    Draft202012Options Draft202012 { get; set; }
    // ... other draft options
}
```

### Output Types

```csharp
namespace FormFinch.JsonSchemaValidation.Validation.Output;

public enum OutputFormat { Flag, Basic, Detailed }

public class OutputUnit
{
    bool Valid { get; }
    string InstanceLocation { get; }
    string KeywordLocation { get; }
    string? AbsoluteKeywordLocation { get; }
    string? Error { get; }
    object? Annotation { get; }
    IList<OutputUnit>? Errors { get; }
    IList<OutputUnit>? Annotations { get; }
}
```

---

## Usage Examples

### Quick Validation

```csharp
// Simplest possible validation
var result = JsonSchemaValidator.Validate(
    """{"type": "string", "minLength": 1}""",
    "\"hello\""
);

if (!result.Valid)
{
    foreach (var error in result.Errors!)
        Console.WriteLine($"{error.InstanceLocation}: {error.Error}");
}
```

### Boolean Check

```csharp
if (JsonSchemaValidator.IsValid("""{"type": "integer"}""", "42"))
    Console.WriteLine("Valid!");
```

### Parsed Schema (Multiple Validations)

```csharp
var schema = JsonSchemaValidator.Parse("""
{
    "type": "object",
    "properties": {
        "name": {"type": "string"},
        "age": {"type": "integer", "minimum": 0}
    },
    "required": ["name"]
}
""");

// Validate many instances efficiently
foreach (var json in jsonDocuments)
{
    if (!schema.IsValid(json))
        Console.WriteLine($"Invalid: {json}");
}
```

### Format Assertion

```csharp
var options = new SchemaValidationOptions
{
    Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
};

var result = JsonSchemaValidator.Validate(
    """{"format": "email"}""",
    "\"not-an-email\"",
    options
);
// result.Valid == false (format is asserted, not just annotated)
```

### DI Integration (ASP.NET Core)

```csharp
// Program.cs
services.AddJsonSchemaValidation(opt =>
{
    opt.Draft202012.FormatAssertionEnabled = true;
});

var app = builder.Build();
app.Services.InitializeSingletonServices();

// In a controller or service
public class ValidationController(
    ISchemaRepository schemaRepository,
    ISchemaValidatorFactory validatorFactory,
    IJsonValidationContextFactory contextFactory)
{
    public IActionResult Validate([FromBody] JsonElement data)
    {
        var validator = validatorFactory.GetValidator(schemaUri);
        var context = contextFactory.CreateContextForRoot(data);
        var result = validator.ValidateBasic(context);
        return result.Valid ? Ok() : BadRequest(result);
    }
}
```

---

## Feature Summary

| Feature | Support |
|---------|---------|
| JSON Schema Draft 2020-12 | ✅ Full |
| JSON Schema Draft 2019-09 | ✅ Full |
| JSON Schema Draft 7 | ✅ Full |
| JSON Schema Draft 6 | ✅ Full |
| JSON Schema Draft 4 | ✅ Full |
| JSON Schema Draft 3 | ✅ Full |
| Output Formats (Flag/Basic/Detailed) | ✅ Spec-compliant |
| Format Validation | ✅ 19 formats |
| $ref / $dynamicRef | ✅ Full |
| unevaluatedProperties/Items | ✅ Full |
| System.Text.Json native | ✅ No external JSON deps |
| Thread-safe | ✅ |
| Compiled validators | ✅ |

---

## Acceptance Criteria Status

- [x] Usability evaluation documented
- [x] List of identified issues/improvements created
- [x] Decision made: **API is ready for release**

---

## Conclusion

The FormFinch.JsonSchemaValidation library now provides an excellent developer experience with:

1. **Low barrier to entry** - Single-line validation with `JsonSchemaValidator.Validate()`
2. **Competitive simplicity** - Matches or exceeds competing libraries in ease of use
3. **Full power when needed** - DI-based API for advanced scenarios
4. **Comprehensive draft support** - All JSON Schema drafts from 3 to 2020-12
5. **Spec-compliant output** - Three output formats per JSON Schema 2020-12 Section 12

The API is ready for 1.0.0 release.
