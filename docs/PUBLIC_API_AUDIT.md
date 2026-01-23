# TASK-004: Public API Surface Audit

**Date:** 2026-01-23
**Status:** Implemented ✓

This document audits the public API surface of FormFinch.JsonSchemaValidation to ensure only intentionally public APIs are exposed.

---

## Executive Summary

The library originally exposed **~50 public types** across 7 namespaces. After implementation:

| Category | Count | Action |
|----------|-------|--------|
| **Intentionally Public** | 20 | Keep public |
| **Made Internal** | ~30 | Implementation completed |

**Key Changes:**
- ~30 implementation detail types changed from `public` to `internal`
- Added `InternalsVisibleTo` for test, compiler, codegen, and benchmark projects
- Discovered that 3 types must remain public for runtime code generation:
  - `FormatValidators` - used by generated code for format validation
  - `ICompiledValidatorRegistry` - used by generated code for registry access
  - `IRegistryAwareCompiledValidator` - used by generated code for validator initialization

---

## Tier Classification

### Tier 1: User-Facing API (Public)

These are the types users directly interact with:

| Type | Namespace | Purpose |
|------|-----------|---------|
| `JsonSchemaValidator` | Root | Static entry point for validation |
| `IJsonSchema` | Root | Parsed schema interface |
| `OutputUnit` | Validation.Output | Spec-compliant validation output |
| `OutputFormat` | Validation.Output | Output format enum (Flag/Basic/Detailed) |
| `SchemaValidationSetup` | DependencyInjection | DI extension methods |
| `SchemaValidationOptions` | DependencyInjection | Main configuration class |
| `Draft202012Options` | DependencyInjection | Draft 2020-12 options |
| `Draft201909Options` | DependencyInjection | Draft 2019-09 options |
| `Draft7Options` | DependencyInjection | Draft 7 options |
| `Draft6Options` | DependencyInjection | Draft 6 options |
| `Draft4Options` | DependencyInjection | Draft 4 options |
| `Draft3Options` | DependencyInjection | Draft 3 options |
| `ServiceProviderExtensions` | DependencyInjection | `InitializeSingletonServices()` |
| `ICompiledValidator` | Abstractions | Interface for user-provided compiled validators |
| `InvalidSchemaException` | Exceptions | Exception users may catch |

### Tier 1.5: Required for Code Generation (Public)

These types must be public because they're referenced by runtime-generated C# code:

| Type | Namespace | Purpose |
|------|-----------|---------|
| `FormatValidators` | Draft202012.Keywords.Format | Static format validation methods for generated code |
| `ICompiledValidatorRegistry` | CompiledValidators | Registry interface for generated validators |
| `IRegistryAwareCompiledValidator` | Abstractions | Interface for validators with external $ref dependencies |
| `CompiledValidatorRegistry` | CompiledValidators | Registry implementation |
| `JsonPointer` | Common | RFC 6901 JSON Pointer (useful utility) |

### Tier 2: Questionable - Evaluate Intent

| Type | Current Use | Recommendation |
|------|-------------|----------------|
| `ISchemaRepository` | Advanced DI example | **Make internal** - Exposes `SchemaMetadata` |
| `ISchemaValidatorFactory` | Advanced DI example | **Make internal** - Returns `ISchemaValidator` |
| `IJsonValidationContextFactory` | Advanced DI example | **Make internal** - Returns `JsonValidationContext` |
| `ValidationResult` | Internal result type | **Make internal** - Users should use `OutputUnit` |
| `JsonPointer` | RFC 6901 utility | **Keep public** - Useful standalone utility |

### Tier 3: Implementation Details (Make Internal)

These types should be `internal`:

#### Abstractions/
| Type | Reason to Make Internal |
|------|------------------------|
| `ISchemaValidator` | Exposes `AddKeywordValidator`, `Validate(context, pointer)` |
| `ISchemaDraftValidatorFactory` | Factory pattern implementation detail |
| `ISchemaFactory` | Internal factory |
| `IJsonValidationContext` | Internal context plumbing |
| `IValidationScope` | Dynamic scope implementation |
| `ILazySchemaValidatorFactory` | Lazy initialization detail |
| `IVocabularyParser` | Internal vocabulary parsing |
| `IVocabularyRegistry` | Internal registry |
| `ISchemaDraftMeta` | Internal metadata |
| `IJsonValidationArrayContext` | Annotation tracking detail |
| `IJsonValidationObjectContext` | Annotation tracking detail |
| ~~`IRegistryAwareCompiledValidator`~~ | ~~Internal compiled validator interface~~ → **Must be public for code gen** |
| `VocabularyParseResult` | Internal parse result |

#### Abstractions/Keywords/
| Type | Reason to Make Internal |
|------|------------------------|
| `IKeywordValidator` | Internal keyword validation |
| `IKeywordValidatorFactory` | Internal factory |
| `ISchemaDraftKeywordValidatorFactory` | Internal draft-specific factory |

#### Common/
| Type | Reason to Make Internal |
|------|------------------------|
| `JsonValidationContext` | Internal context implementation |
| `ValidationScope` | Internal scope implementation |

#### Repositories/
| Type | Reason to Make Internal |
|------|------------------------|
| `SchemaRepository` | Implementation of `ISchemaRepository` |
| `SchemaMetadata` | Internal schema data structure |

#### CompiledValidators/
| Type | Status |
|------|--------|
| ~~`ICompiledValidatorRegistry`~~ | **Must be public** - Referenced by generated code |
| `CompiledValidatorRegistry` | **Must be public** - Implementation used by generated code |

---

## Impact Analysis

### Breaking Changes If Made Internal

Making these types internal would be a **breaking change** for users who:
1. Use the advanced DI pattern to directly inject `ISchemaRepository`, `ISchemaValidatorFactory`, or `IJsonValidationContextFactory`
2. Implement custom keyword validators (rare)
3. Access `SchemaMetadata` directly

### Mitigation Strategy

1. **For advanced DI users:** The static `JsonSchemaValidator` API and `IJsonSchema.Parse()` cover 99% of use cases. Users who need DI can inject a wrapper service.

2. **For the DI example in API_USABILITY_EVALUATION.md:** Update to show a simpler pattern:

```csharp
// Before (exposes internal types)
public class MyService(ISchemaRepository repo, ISchemaValidatorFactory factory, IJsonValidationContextFactory contextFactory)
{
    public bool ValidateUser(JsonElement userData)
    {
        var validator = factory.GetValidator(userSchemaUri);
        var context = contextFactory.CreateContextForRoot(userData);
        return validator.IsValid(context);
    }
}

// After (clean public API)
public class MyService
{
    private readonly IJsonSchema _userSchema;

    public MyService()
    {
        _userSchema = JsonSchemaValidator.Parse(UserSchemaJson);
    }

    public bool ValidateUser(JsonElement userData)
    {
        return _userSchema.IsValid(userData);
    }
}
```

---

## Detailed Audit by Namespace

### FormFinch.JsonSchemaValidation (Root)

| Type | Visibility | Decision |
|------|------------|----------|
| `JsonSchemaValidator` | public | **Keep** - Main entry point |
| `IJsonSchema` | public | **Keep** - Parsed schema interface |
| `CompiledJsonSchema` | internal | Already internal |

### FormFinch.JsonSchemaValidation.Validation

| Type | Visibility | Decision |
|------|------------|----------|
| `ValidationResult` | public | **Make internal** - Internal type, users get `OutputUnit` |

### FormFinch.JsonSchemaValidation.Validation.Output

| Type | Visibility | Decision |
|------|------------|----------|
| `OutputFormat` | public | **Keep** - User-facing enum |
| `OutputUnit` | public | **Keep** - User-facing output type |

### FormFinch.JsonSchemaValidation.DependencyInjection

| Type | Visibility | Decision |
|------|------------|----------|
| `SchemaValidationSetup` | public | **Keep** - DI entry point |
| `SchemaValidationOptions` | public | **Keep** - Configuration |
| `Draft*Options` (6 classes) | public | **Keep** - Configuration |
| `ServiceProviderExtensions` | public | **Keep** - Required for initialization |

### FormFinch.JsonSchemaValidation.Abstractions

| Type | Visibility | Decision |
|------|------------|----------|
| `ISchemaValidator` | public | **Make internal** |
| `ISchemaRepository` | public | **Make internal** |
| `ISchemaValidatorFactory` | public | **Make internal** |
| `ISchemaDraftValidatorFactory` | public | **Make internal** |
| `ISchemaFactory` | public | **Make internal** |
| `IJsonValidationContext` | public | **Make internal** |
| `IJsonValidationContextFactory` | public | **Make internal** |
| `IValidationScope` | public | **Make internal** |
| `ICompiledValidator` | public | **Keep** - User-implemented interface |
| `IRegistryAwareCompiledValidator` | public | **Make internal** |
| `ILazySchemaValidatorFactory` | public | **Make internal** |
| `IVocabularyParser` | public | **Make internal** |
| `IVocabularyRegistry` | public | **Make internal** |
| `ISchemaDraftMeta` | public | **Make internal** |
| `IJsonValidationArrayContext` | public | **Make internal** |
| `IJsonValidationObjectContext` | public | **Make internal** |
| `VocabularyParseResult` | public | **Make internal** |

### FormFinch.JsonSchemaValidation.Abstractions.Keywords

| Type | Visibility | Decision |
|------|------------|----------|
| `IKeywordValidator` | public | **Make internal** |
| `IKeywordValidatorFactory` | public | **Make internal** |
| `ISchemaDraftKeywordValidatorFactory` | public | **Make internal** |

### FormFinch.JsonSchemaValidation.Common

| Type | Visibility | Decision |
|------|------------|----------|
| `JsonPointer` | public | **Keep** - Useful RFC 6901 utility |
| `JsonValidationContext` | public | **Make internal** |
| `ValidationScope` | public | **Make internal** |

### FormFinch.JsonSchemaValidation.Repositories

| Type | Visibility | Decision |
|------|------------|----------|
| `SchemaRepository` | public | **Make internal** |
| `SchemaMetadata` | public | **Make internal** |

### FormFinch.JsonSchemaValidation.CompiledValidators

| Type | Visibility | Decision |
|------|------------|----------|
| `ICompiledValidatorRegistry` | public | **Make internal** |

### FormFinch.JsonSchemaValidation.Exceptions

| Type | Visibility | Decision |
|------|------------|----------|
| `InvalidSchemaException` | public | **Keep** - User-catchable exception |

---

## Final Public API Surface (After Cleanup)

After making recommended changes, the public API would be:

### Root Namespace
```csharp
public static class JsonSchemaValidator
{
    static OutputUnit Validate(string schema, string instance, OutputFormat format = Basic);
    static OutputUnit Validate(JsonElement schema, JsonElement instance, OutputFormat format = Basic);
    static OutputUnit Validate(string schema, string instance, SchemaValidationOptions options, OutputFormat format = Basic);
    static OutputUnit Validate(JsonElement schema, JsonElement instance, SchemaValidationOptions options, OutputFormat format = Basic);
    static bool IsValid(string schema, string instance);
    static bool IsValid(JsonElement schema, JsonElement instance);
    static IJsonSchema Parse(string schema);
    static IJsonSchema Parse(JsonElement schema);
    static IJsonSchema Parse(string schema, SchemaValidationOptions options);
    static IJsonSchema Parse(JsonElement schema, SchemaValidationOptions options);
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

### Validation.Output Namespace
```csharp
public enum OutputFormat { Flag, Basic, Detailed }

public class OutputUnit
{
    required bool Valid { get; set; }
    required string InstanceLocation { get; set; }
    required string KeywordLocation { get; set; }
    string? AbsoluteKeywordLocation { get; set; }
    string? Error { get; set; }
    object? Annotation { get; set; }
    IList<OutputUnit>? Errors { get; set; }
    IList<OutputUnit>? Annotations { get; set; }
}
```

### DependencyInjection Namespace
```csharp
public static class SchemaValidationSetup
{
    static IServiceCollection AddJsonSchemaValidation(this IServiceCollection services, Action<SchemaValidationOptions>? setupAction = null);
    static IServiceCollection AddCompiledValidators(this IServiceCollection services, IEnumerable<ICompiledValidator> validators);
    static IServiceCollection AddCompiledValidators(this IServiceCollection services, params ICompiledValidator[] validators);
}

public static class ServiceProviderExtensions
{
    static void InitializeSingletonServices(this IServiceProvider serviceProvider);
}

public class SchemaValidationOptions { /* configuration properties */ }
public class Draft202012Options { bool FormatAssertionEnabled { get; set; } }
public class Draft201909Options { bool FormatAssertionEnabled { get; set; } }
public class Draft7Options { bool FormatAssertionEnabled { get; set; } bool ContentAssertionEnabled { get; set; } }
public class Draft6Options { bool FormatAssertionEnabled { get; set; } }
public class Draft4Options { bool FormatAssertionEnabled { get; set; } }
public class Draft3Options { bool FormatAssertionEnabled { get; set; } }
```

### Abstractions Namespace
```csharp
public interface ICompiledValidator
{
    Uri SchemaUri { get; }
    bool IsValid(JsonElement instance);
}
```

### Common Namespace
```csharp
public sealed class JsonPointer
{
    static JsonPointer Empty { get; }
    static JsonPointer Parse(string pointer);
    JsonPointer Append(string segment);
    JsonPointer Append(int index);
    JsonPointer Parent();
    string ToString();
}
```

### Exceptions Namespace
```csharp
public class InvalidSchemaException : Exception { /* standard exception constructors */ }
```

---

## Acceptance Criteria

- [x] All public types intentionally public (documented above)
- [x] Implementation details identified for making internal (~36 types)
- [x] Public API design decisions documented (this document)

---

## Implementation Notes

### Changes Made (2026-01-23)

**Option 1 was selected and implemented.** The following changes were made:

1. **Made ~30 types internal:**
   - All interfaces in `Abstractions/` except `ICompiledValidator`, `IRegistryAwareCompiledValidator`
   - All keyword-related interfaces in `Abstractions/Keywords/`
   - All types in `Common/` except `JsonPointer`
   - All types in `Repositories/`
   - `ValidationResult` in `Validation/`
   - All draft-specific factories, setup classes, and meta classes

2. **Added InternalsVisibleTo in .csproj files:**
   - `JsonSchemaValidation.csproj`: Tests, CodeGeneration, Compiler, Benchmarks
   - `JsonSchemaValidation.Compiler.csproj`: Tests, Benchmarks

3. **Types that must remain public for code generation:**
   - `FormatValidators` - Generated code calls static format validation methods
   - `ICompiledValidatorRegistry` - Generated code implements registry-aware initialization
   - `IRegistryAwareCompiledValidator` - Generated validators implement this interface
   - `CompiledValidatorRegistry` - Registry implementation used by generated code

4. **All 2339 tests pass** after implementation.
