# TASK-004a: API Usability Evaluation

**Date:** 2026-01-23
**Status:** Complete

This document evaluates the usability of the FormFinch.JsonSchemaValidation public API before its 1.0.0 release.

---

## Executive Summary

The library provides a **three-tier API** designed for different user needs:

| Tier | Entry Point | Use Case |
|------|-------------|----------|
| **Simple** | `JsonSchemaValidator.Validate()` | One-off validation, scripts, quick checks |
| **Efficient** | `JsonSchemaValidator.Parse()` → `IJsonSchema` | Repeated validation against same schema |
| **Advanced** | `AddJsonSchemaValidation()` + DI | Enterprise apps, ASP.NET Core, full control |

All evaluation criteria pass. The API is **ready for 1.0.0 release**.

---

## Evaluation Criteria

### 1. Discoverability ✅ Excellent

**Question:** Can a new user find the entry point easily?

**Finding:** The static `JsonSchemaValidator` class is the obvious starting point.

```csharp
using FormFinch.JsonSchemaValidation;

// IntelliSense immediately shows: Validate, IsValid, Parse
var result = JsonSchemaValidator.Validate(schema, instance);
```

**Strengths:**
- Single namespace import for basic usage
- Static class with clear method names
- No setup required for simple cases
- Method overloads guide users to the right signature

### 2. Simplicity ✅ Excellent

**Question:** Is the common case simple?

**Minimum code for validation:**

```csharp
// 1 line - validate and get result
var result = JsonSchemaValidator.Validate("""{"type": "string"}""", "\"hello\"");

// 1 line - boolean check
bool valid = JsonSchemaValidator.IsValid("""{"type": "string"}""", "\"hello\"");
```

**Comparison with competing libraries:**

| Library | Lines for First Validation |
|---------|---------------------------|
| **FormFinch** | 1 |
| JsonSchema.Net | 2 |
| NJsonSchema | 2-3 |
| Newtonsoft.Json.Schema | 2-3 |

**Progressive complexity:** Users can start simple and adopt more advanced patterns as needed:

```csharp
// Level 1: One-liner
var result = JsonSchemaValidator.Validate(schema, instance);

// Level 2: Reusable schema (better performance)
var schema = JsonSchemaValidator.Parse(schemaJson);
var result1 = schema.Validate(instance1);
var result2 = schema.Validate(instance2);

// Level 3: Custom options
var options = new SchemaValidationOptions {
    Draft202012 = { FormatAssertionEnabled = true }
};
var result = JsonSchemaValidator.Validate(schema, instance, options);

// Level 4: Full DI integration
services.AddJsonSchemaValidation(opt => { ... });
```

### 3. Consistency ✅ Good

**Question:** Are naming conventions consistent throughout?

**Findings:**

| Pattern | Examples |
|---------|----------|
| Validation methods | `Validate()`, `IsValid()` |
| Factory methods | `Parse()`, `CreateContextForRoot()` |
| Options classes | `SchemaValidationOptions`, `Draft202012Options` |
| Interfaces | `IJsonSchema`, `ISchemaValidator`, `ICompiledValidator` |

**Consistent behaviors:**
- All `Validate()` methods return `OutputUnit`
- All `IsValid()` methods return `bool`
- All `Parse()` methods return `IJsonSchema`
- String overloads always have `JsonElement` counterparts

### 4. Documentation ✅ Good

**Question:** Are public APIs self-documenting with good names?

**Method names are intuitive:**
- `Validate` → validates and returns detailed result
- `IsValid` → validates and returns boolean
- `Parse` → parses schema for reuse

**Output properties are clear:**
- `Valid` → whether validation passed
- `InstanceLocation` → where in the JSON the error occurred
- `KeywordLocation` → which schema keyword failed
- `Error` → human-readable error message

**XML documentation is comprehensive:**
- All public methods have `<summary>` tags
- Parameters documented with `<param>` tags
- Examples provided with `<example>` tags

### 5. Flexibility ✅ Excellent

**Question:** Can advanced users customize behavior without fighting the API?

**Customization options:**

| Need | Solution |
|------|----------|
| Different output detail level | `OutputFormat.Flag`, `Basic`, `Detailed` |
| Enable format validation | `Draft202012Options.FormatAssertionEnabled = true` |
| Disable specific drafts | `EnableDraft7 = false` |
| DI integration | `AddJsonSchemaValidation()` |
| Custom compiled validators | Implement `ICompiledValidator` |
| Pre-register schemas | Use DI API with `ISchemaRepository` |

**No dead ends:** Users can always escalate from simple API to DI-based API without rewriting code.

### 6. Error Handling ✅ Good

**Question:** Are errors clear and actionable?

**Validation errors include:**
- `InstanceLocation` - JSON Pointer to the invalid value (e.g., `/users/0/email`)
- `KeywordLocation` - JSON Pointer to the schema keyword (e.g., `/properties/email/format`)
- `AbsoluteKeywordLocation` - Full URI for debugging complex schemas
- `Error` - Human-readable message (e.g., "Value does not match format 'email'")

**Output formats for different needs:**

| Format | Use Case |
|--------|----------|
| `Flag` | High-performance, just need pass/fail |
| `Basic` | API responses, flat error list |
| `Detailed` | Debugging, hierarchical structure matching schema |

**Invalid input handling:**
- Invalid JSON throws `JsonException` (standard .NET behavior)
- Invalid schema returns `Valid = false` with error message

---

## Public API Summary

### Static API (Primary)

```csharp
public static class JsonSchemaValidator
{
    // One-shot validation
    static OutputUnit Validate(string schema, string instance, OutputFormat format = Basic);
    static OutputUnit Validate(JsonElement schema, JsonElement instance, OutputFormat format = Basic);
    static OutputUnit Validate(string schema, string instance, SchemaValidationOptions options, OutputFormat format = Basic);
    static OutputUnit Validate(JsonElement schema, JsonElement instance, SchemaValidationOptions options, OutputFormat format = Basic);

    // Boolean validation (fast path)
    static bool IsValid(string schema, string instance);
    static bool IsValid(JsonElement schema, JsonElement instance);

    // Parse for reuse
    static IJsonSchema Parse(string schema);
    static IJsonSchema Parse(JsonElement schema);
    static IJsonSchema Parse(string schema, SchemaValidationOptions options);
    static IJsonSchema Parse(JsonElement schema, SchemaValidationOptions options);
}
```

### Parsed Schema Interface

```csharp
public interface IJsonSchema
{
    Uri SchemaUri { get; }
    OutputUnit Validate(string instance, OutputFormat format = Basic);
    OutputUnit Validate(JsonElement instance, OutputFormat format = Basic);
    bool IsValid(string instance);
    bool IsValid(JsonElement instance);
}
```

### Output Types

```csharp
public enum OutputFormat { Flag, Basic, Detailed }

public class OutputUnit
{
    bool Valid { get; }
    string InstanceLocation { get; }   // JSON Pointer
    string KeywordLocation { get; }    // JSON Pointer
    string? AbsoluteKeywordLocation { get; }
    string? Error { get; }
    object? Annotation { get; }
    IList<OutputUnit>? Errors { get; }      // For Detailed format
    IList<OutputUnit>? Annotations { get; } // For Detailed format
}
```

### Configuration

```csharp
public class SchemaValidationOptions
{
    string DefaultDraftVersion { get; set; }
    bool EnableDraft202012 { get; set; } = true;
    bool EnableDraft201909 { get; set; } = true;
    bool EnableDraft7 { get; set; } = true;
    bool EnableDraft6 { get; set; } = true;
    bool EnableDraft4 { get; set; } = true;
    bool EnableDraft3 { get; set; } = true;

    Draft202012Options Draft202012 { get; set; }
    // ... other draft options
}

public class Draft202012Options
{
    bool FormatAssertionEnabled { get; set; } = false;
}
```

### DI Integration

```csharp
// Setup
services.AddJsonSchemaValidation(options => {
    options.Draft202012.FormatAssertionEnabled = true;
});

// After building service provider
serviceProvider.InitializeSingletonServices();

// Inject and use
public class MyService(ISchemaRepository repo, ISchemaValidatorFactory factory, IJsonValidationContextFactory contextFactory)
{
    public bool ValidateUser(JsonElement userData)
    {
        var validator = factory.GetValidator(userSchemaUri);
        var context = contextFactory.CreateContextForRoot(userData);
        return validator.IsValid(context);
    }
}
```

---

## Usage Examples

### Quick Validation

```csharp
var result = JsonSchemaValidator.Validate(
    """{"type": "object", "required": ["name"]}""",
    """{"name": "Alice"}"""
);

Console.WriteLine(result.Valid ? "Valid!" : result.Errors![0].Error);
```

### Repeated Validation

```csharp
var schema = JsonSchemaValidator.Parse("""
{
    "type": "object",
    "properties": {
        "email": {"type": "string", "format": "email"}
    },
    "required": ["email"]
}
""");

foreach (var user in users)
{
    if (!schema.IsValid(user))
        Console.WriteLine($"Invalid user: {user}");
}
```

### Format Assertion

```csharp
var options = new SchemaValidationOptions
{
    Draft202012 = new Draft202012Options { FormatAssertionEnabled = true }
};

// Now "format" keyword will validate, not just annotate
var result = JsonSchemaValidator.Validate(
    """{"format": "email"}""",
    "\"not-an-email\"",
    options
);
// result.Valid == false
```

### Detailed Error Reporting

```csharp
var result = JsonSchemaValidator.Validate(
    schema,
    instance,
    OutputFormat.Detailed
);

void PrintErrors(OutputUnit unit, int indent = 0)
{
    var prefix = new string(' ', indent * 2);
    if (!unit.Valid && unit.Error != null)
        Console.WriteLine($"{prefix}{unit.InstanceLocation}: {unit.Error}");

    if (unit.Errors != null)
        foreach (var child in unit.Errors)
            PrintErrors(child, indent + 1);
}

PrintErrors(result);
```

---

## Performance Characteristics

| Pattern | Overhead | Use When |
|---------|----------|----------|
| `Validate(string, string)` | Schema parsed + registered each call* | Single validation |
| `Parse()` + `Validate()` | Schema parsed once, validation is fast | Repeated validation |
| DI + `ISchemaValidator` | Minimal, validators cached | High-throughput apps |

*Schema caching by content hash prevents unbounded memory growth.

---

## Feature Matrix

| Feature | Support |
|---------|---------|
| Draft 2020-12 | ✅ Full |
| Draft 2019-09 | ✅ Full |
| Draft 7 | ✅ Full |
| Draft 6 | ✅ Full |
| Draft 4 | ✅ Full |
| Draft 3 | ✅ Full |
| `$ref` / `$dynamicRef` | ✅ Full |
| `unevaluatedProperties` / `unevaluatedItems` | ✅ Full |
| Format validation (19 formats) | ✅ Optional |
| Output formats (Flag/Basic/Detailed) | ✅ Spec-compliant |
| System.Text.Json (no external deps) | ✅ |
| Thread-safe | ✅ |
| Schema caching | ✅ Content-hash based |

---

## Acceptance Criteria

- [x] **Usability evaluation documented** - This document
- [x] **List of identified issues/improvements** - None blocking; API is ready
- [x] **Decision made** - API is ready for 1.0.0 release

---

## Conclusion

The FormFinch.JsonSchemaValidation API provides:

1. **Zero-friction entry** - One-line validation without any setup
2. **Progressive complexity** - Simple → Efficient → Advanced paths
3. **Competitive simplicity** - Matches or exceeds alternatives
4. **Full spec compliance** - All drafts, all output formats
5. **Performance-conscious design** - Caching, fast paths, DI integration

**Recommendation:** Proceed with 1.0.0 release. No API changes required.
