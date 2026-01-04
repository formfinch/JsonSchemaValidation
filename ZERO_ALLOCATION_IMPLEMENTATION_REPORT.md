# Zero-Allocation JSON Schema Validation: Implementation Report

## Executive Summary

An attempt was made to replace the existing `JsonElement`-based JSON Schema 2020-12 validator with a streaming, near-zero-allocation implementation using `Utf8JsonReader`. The implementation failed, achieving only 71% test pass rate (355/496 tests) before rollback.

This report documents what went wrong, lessons learned, and a revised approach for future attempts.

---

## Part 1: What Went Wrong

### 1.1 Fundamental Architectural Mistake

**The Problem:** `Utf8JsonReader` is forward-only and cannot be rewound. JSON Schema validation requires multiple passes over the same data for composition keywords (`allOf`, `anyOf`, `oneOf`, `if/then/else`).

**The Flawed Solution:** Each validator that needed to re-read data would:
1. Serialize the current value to a `byte[]` using `MemoryStream` + `Utf8JsonWriter`
2. Create new `Utf8JsonReader` instances from those bytes
3. Validate each sub-schema against the copied bytes

```csharp
// This pattern appeared in AllOfSpanValidator, AnyOfSpanValidator, OneOfSpanValidator, etc.
using var stream = new MemoryStream();           // ALLOCATION
using var writer = new Utf8JsonWriter(stream);   // ALLOCATION
CopyValue(ref reader, writer);
var valueBytes = stream.ToArray();               // ALLOCATION

foreach (var subSchema in _validators)
{
    var subReader = new Utf8JsonReader(valueBytes);
    subSchema.Validate(ref subReader, ref context);
}
```

**Why This Failed:**
- Every composition keyword allocated new byte arrays
- Nested composition keywords compounded allocations
- For large documents, this was worse than the original implementation
- The "zero allocation" promise was completely broken

### 1.2 Premature Deletion of Working Code

**The Mistake:** 134 files (validators, factories, context classes) were deleted before the replacement was complete and tested.

**The Consequence:**
- No easy way to compare behavior between old and new implementations
- No fallback when issues were discovered
- Debugging became harder without a working reference

**What Should Have Happened:**
- Keep both implementations side-by-side
- Run both against the test suite
- Only delete old code after new code achieves 100% parity

### 1.3 No Incremental Testing

**The Mistake:** Multiple validators were implemented before running tests. When tests failed, it was unclear which validator was at fault.

**The Consequence:**
- Errors compounded across validators
- Root causes were masked by downstream failures
- Debugging became a game of whack-a-mole

**What Should Have Happened:**
- Implement one keyword validator at a time
- Run the full test suite after each validator
- Achieve 100% pass rate for that keyword before moving to the next

### 1.4 Annotations Were Never Implemented

**The Problem:** JSON Schema 2020-12 requires annotation collection. Keywords like `properties`, `prefixItems`, and `contains` must report what they evaluated so that `unevaluatedProperties` and `unevaluatedItems` can function correctly.

**What Was Implemented:** Validators returned only `bool` (valid/invalid).

**What Was Needed:** Validators needed to also report:
- Which properties were evaluated
- Which array indices were evaluated
- Other keyword-specific annotations

**The Consequence:** `unevaluatedProperties` and `unevaluatedItems` could never work correctly.

### 1.5 Dynamic Scope Not Addressed

**The Problem:** `$dynamicRef` requires tracking the dynamic scope—which schemas are in the call stack at runtime—to resolve references correctly.

**What Was Implemented:** Static resolution at schema compilation time.

**What Was Needed:** Runtime scope tracking that follows the validation call stack.

### 1.6 Misunderstanding of "Zero Allocation"

**Initial Assumption:** Validation could be performed with zero heap allocations by using `Utf8JsonReader` directly on the input bytes.

**Reality:**
- Forward-only readers cannot support multi-pass validation
- Buffering data to enable re-reading defeats zero-allocation
- Some allocations are unavoidable (error messages, property tracking)

**Correct Understanding:** "Near-zero allocation" means:
- No allocation of the input data (caller provides bytes)
- Pooled/reused allocations for tracking state
- Stack allocation where possible
- Minimal per-validation allocations

---

## Part 2: Correct Approach for Future Implementation

### 2.1 Core Insight: Position-Based Re-reading

Instead of copying bytes, track positions and re-read from the original source.

**For `ReadOnlyMemory<byte>` input:**
```csharp
public ref struct ValidationState
{
    public ReadOnlyMemory<byte> Source;
    public int CurrentOffset;

    public Utf8JsonReader CreateReaderAtOffset(int offset)
    {
        // Zero allocation - just slicing memory
        return new Utf8JsonReader(Source.Span[offset..]);
    }

    public int SavePosition() => CurrentOffset;

    public void RestorePosition(int offset) => CurrentOffset = offset;
}
```

**For seekable streams:**
```csharp
public void RestorePosition(long offset)
{
    _stream.Seek(offset, SeekOrigin.Begin);
}
```

### 2.2 Input Requirements

The implementation should support multiple input types with different capability levels:

| Input Type | Seekable | Zero-Copy | Full Compliance |
|------------|----------|-----------|-----------------|
| `ReadOnlyMemory<byte>` | Yes (slice) | Yes | Yes |
| `ReadOnlySpan<byte>` | Yes (slice) | Yes | Yes |
| Seekable `Stream` | Yes (seek) | No | Yes |
| Non-seekable `Stream` | No | No | Limited* |

*Limited compliance: Only single-pass schemas supported, or must buffer to memory.

### 2.3 Revised Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Entry Point                             │
│  ValidateAsync(Stream) / Validate(ReadOnlyMemory<byte>)     │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                   ValidationContext                          │
│  - Source bytes/stream reference                             │
│  - Position tracking (stack of saved positions)              │
│  - Pooled annotation collector                               │
│  - Pooled error collector                                    │
│  - Pooled property tracker (for unevaluated*)               │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                  Schema Validator                            │
│  - Pre-compiled keyword validators                           │
│  - All strings pre-encoded as UTF-8 bytes                   │
│  - Execution order guaranteed                                │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│               Keyword Validators                             │
│  - Receive context with source reference                     │
│  - Save/restore positions for multi-pass                     │
│  - Report annotations to context                             │
│  - Report errors to pooled collector                         │
└─────────────────────────────────────────────────────────────┘
```

### 2.4 Keyword Validator Interface

```csharp
public interface IKeywordValidator
{
    /// <summary>
    /// Validates the current value in the context.
    /// </summary>
    /// <param name="context">
    /// Mutable context with source reference, position tracking,
    /// and pooled collectors for annotations and errors.
    /// </param>
    /// <returns>True if valid, false otherwise.</returns>
    bool Validate(ref ValidationContext context);
}
```

### 2.5 Composition Keyword Pattern

```csharp
internal sealed class AllOfValidator : IKeywordValidator
{
    private readonly ISchemaValidator[] _subSchemas;

    public bool Validate(ref ValidationContext context)
    {
        // Save position before validating
        int savedPosition = context.SavePosition();
        bool allValid = true;

        for (int i = 0; i < _subSchemas.Length; i++)
        {
            if (i > 0)
            {
                // Restore to start of value for subsequent schemas
                context.RestorePosition(savedPosition);
            }

            if (!_subSchemas[i].Validate(ref context))
            {
                allValid = false;
                if (context.StopOnFirstError) break;
            }
        }

        return allValid;
    }
}
```

### 2.6 Annotation Collection

```csharp
public ref struct ValidationContext
{
    // Pooled from ArrayPool<string>
    public AnnotationCollector Annotations;

    public void AnnotateEvaluatedProperty(ReadOnlySpan<byte> propertyName)
    {
        Annotations.AddEvaluatedProperty(propertyName);
    }

    public void AnnotateEvaluatedIndex(int index)
    {
        Annotations.AddEvaluatedIndex(index);
    }

    public bool WasPropertyEvaluated(ReadOnlySpan<byte> propertyName)
    {
        return Annotations.ContainsProperty(propertyName);
    }
}
```

### 2.7 Pooling Strategy

```csharp
public sealed class ValidationContextPool
{
    private readonly ObjectPool<AnnotationCollector> _annotationPool;
    private readonly ObjectPool<ErrorCollector> _errorPool;
    private readonly ObjectPool<HashSet<int>> _indexSetPool;

    public ValidationContext Rent(ReadOnlyMemory<byte> source)
    {
        return new ValidationContext
        {
            Source = source,
            Annotations = _annotationPool.Get(),
            Errors = _errorPool.Get(),
            // ... etc
        };
    }

    public void Return(ValidationContext context)
    {
        context.Annotations.Clear();
        context.Errors.Clear();
        _annotationPool.Return(context.Annotations);
        _errorPool.Return(context.Errors);
    }
}
```

### 2.8 Implementation Order

Implement in this order, testing after each step:

1. **Foundation**
   - [ ] `ValidationContext` ref struct with position tracking
   - [ ] `ValidationContextPool` for pooled resources
   - [ ] Basic test harness comparing old vs new implementation

2. **Simple Validators (No Re-reading Required)**
   - [ ] `type` - single token check
   - [ ] `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`
   - [ ] `minLength`, `maxLength`
   - [ ] `pattern`
   - [ ] `minItems`, `maxItems`
   - [ ] `minProperties`, `maxProperties`
   - [ ] `required`
   - [ ] `const`, `enum`

3. **Object Validators (Single Pass)**
   - [ ] `properties` - with annotation collection
   - [ ] `patternProperties` - with annotation collection
   - [ ] `additionalProperties` - using annotations from above
   - [ ] `propertyNames`
   - [ ] `dependentRequired`, `dependentSchemas`

4. **Array Validators (Single Pass)**
   - [ ] `prefixItems` - with annotation collection
   - [ ] `items` - using annotations from prefixItems
   - [ ] `contains`, `minContains`, `maxContains`
   - [ ] `uniqueItems`

5. **Composition Validators (Position-Based Re-reading)**
   - [ ] `allOf`
   - [ ] `anyOf`
   - [ ] `oneOf`
   - [ ] `not`
   - [ ] `if`/`then`/`else`

6. **Reference Validators**
   - [ ] `$ref` - static resolution
   - [ ] `$dynamicRef` - with scope tracking

7. **Unevaluated Validators (Depend on Annotations)**
   - [ ] `unevaluatedProperties`
   - [ ] `unevaluatedItems`

8. **Format Validators**
   - [ ] All 19 format validators with assertion mode support

9. **Integration**
   - [ ] Wire up DI registration
   - [ ] Performance benchmarks vs old implementation
   - [ ] Memory allocation benchmarks

### 2.9 Testing Strategy

1. **After each validator:** Run full test suite, must maintain 100% pass rate
2. **Comparison testing:** Run both old and new implementations, compare results
3. **Allocation tracking:** Use `dotnet-counters` or BenchmarkDotNet to verify allocation counts
4. **Stress testing:** Large documents, deeply nested schemas, recursive schemas

### 2.10 Success Criteria

Before replacing the old implementation:

- [ ] 100% test pass rate (473/473 tests)
- [ ] All keyword validators implemented
- [ ] Annotation collection working correctly
- [ ] Dynamic scope tracking for `$dynamicRef`
- [ ] Benchmarks show improvement in allocation count
- [ ] No regression in validation throughput
- [ ] Documentation updated

---

## Part 3: Allocation Budget

### Unavoidable Allocations

| Category | When | Mitigation |
|----------|------|------------|
| Error messages | On validation failure | Pool error collectors, defer string creation |
| Property name tracking | `unevaluatedProperties` | Pool HashSet, use span-based keys |
| Index tracking | `unevaluatedItems` | Pool BitArray or int[] |
| Regex matches | `pattern` keyword | Cache compiled Regex at schema registration |
| String conversion | Property name comparison | Pre-encode schema strings as UTF-8 |

### Zero-Allocation Operations

| Operation | Technique |
|-----------|-----------|
| Reading JSON tokens | `Utf8JsonReader` on span slice |
| Position save/restore | Stack-allocated int/long |
| Type checking | Direct token comparison |
| Numeric comparison | `reader.GetDecimal()` (no allocation) |
| String length | UTF-8 byte counting on span |
| Property name matching | Span-to-span comparison |

---

## Part 4: Risk Mitigation

### Risk: Non-seekable Stream Input

**Mitigation:**
- Document limitation clearly
- Provide `ValidateBuffered(Stream)` that reads to pooled buffer first
- For small documents, buffer automatically
- For large documents, require seekable stream or pre-loaded bytes

### Risk: Deep Recursion Stack Overflow

**Mitigation:**
- Track recursion depth in context
- Configurable max depth (default 64)
- Fail gracefully with clear error

### Risk: Performance Regression

**Mitigation:**
- Benchmark before replacing old implementation
- Keep old implementation available behind feature flag initially
- Profile hot paths with dotTrace or similar

### Risk: Specification Compliance Gaps

**Mitigation:**
- Run official JSON Schema Test Suite after each change
- Compare annotation output between implementations
- Review spec sections for each keyword during implementation

---

## Conclusion

The failed implementation attempt provided valuable lessons:

1. **Don't break what works** - keep old implementation until new one is proven
2. **Test incrementally** - verify after each validator
3. **Understand the problem** - multi-pass validation needs position tracking, not data copying
4. **"Zero allocation" is nuanced** - minimize allocations, pool what you can't eliminate
5. **The spec is complex** - annotations, dynamic scope, and unevaluated keywords require careful design

A successful implementation is achievable with the position-based re-reading approach, proper pooling, and disciplined incremental development.
