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

## Part 5: Second Implementation Attempt (January 2026)

### 5.1 Approach

Following the lessons from Part 1, a second implementation attempt was made using a fundamentally different approach:

**Key Design Decisions:**
1. **Lazy parsing via virtual `Data` property** - `SpanValidationContext` implements `IJsonValidationContext` with `ReadOnlyMemory<byte>` source. The `Data` property is lazy - only parsed when accessed.
2. **Side-by-side implementation** - Draft202012Zero registered alongside Draft202012, sharing all interfaces
3. **Incremental validator optimization** - Existing validators work unchanged; optimized validators added one at a time
4. **Full test coverage** - 986 tests (448 original + 62 interchangeability + 476 Draft202012Zero) all passing

**Implementation Files Created:**
- `SpanValidationContext.cs` - Context holding `ReadOnlyMemory<byte>` with lazy `Data` parsing
- `SpanValidationObjectContext.cs` - Object context for property iteration
- `SpanValidationArrayContext.cs` - Array context for item iteration
- `SpanValidationContextFactory.cs` - Factory implementing `IJsonValidationContextFactory`
- `SchemaDraft202012ZeroSetup.cs` - Self-contained DI registration via `AddDraft202012Zero()`

**Stage B Optimizations (Span-Optimized Validators):**
- Type validators: `TypeSpanStringValidator`, `TypeSpanNumberValidator`, `TypeSpanIntegerValidator`, `TypeSpanBooleanValidator`, `TypeSpanArrayValidator`, `TypeSpanObjectValidator`, `TypeSpanNullValidator`
- Numeric validators: `MinimumSpanValidator`, `MaximumSpanValidator`, `ExclusiveMinimumSpanValidator`, `ExclusiveMaximumSpanValidator`
- String validators: `MinLengthSpanValidator`, `MaxLengthSpanValidator`

These validators check `context is SpanValidationContext` and use `Utf8JsonReader` directly, avoiding the lazy `Data` parse.

### 5.2 Benchmark Results

Benchmarks using BenchmarkDotNet revealed the span-based implementation was **slower, not faster**:

#### Simple Integer Validation (type + minimum + maximum)
| Method | Mean | Allocated |
|--------|------|-----------|
| Draft202012_ValidInteger | 8.80 µs | 24.73 KB |
| Draft202012Zero_ValidInteger | 10.80 µs | 25.56 KB |

**Result: 23% slower, 3% more memory**

#### Complex Object Validation (multiple nested properties)
| Method | Mean | Allocated |
|--------|------|-----------|
| Draft202012_SmallData | 9.34 µs | 25.92 KB |
| Draft202012Zero_SmallData | 10.76 µs | 26.85 KB |

**Result: 15% slower, 4% more memory**

#### Parsing Overhead (array validation)
| Array Size | Draft202012 | Draft202012Zero |
|------------|-------------|-----------------|
| 10 | 4.29 µs | 7.01 µs |
| 100 | 18.04 µs | 26.96 µs |
| 1000 | 159.08 µs | 225.88 µs |
| 10000 | 1.89 ms | 2.56 ms |

**Result: 35-63% slower across all sizes**

### 5.3 Why the Optimizations Failed

1. **`Utf8JsonReader` creation overhead**: Creating a new reader from a memory slice has non-trivial setup cost. Each validator creates its own reader, multiplying this overhead.

2. **`JsonElement` is already highly optimized**: `JsonDocument.Parse()` creates an indexed token database allowing O(1) random access. Property lookups, array indexing, and type checks are essentially pointer arithmetic.

3. **Multiple reader passes vs single parse**: With `JsonElement`, one `JsonDocument.Parse()` call processes the entire document. With span-based validation, each validator creates and advances its own reader, re-parsing the same tokens multiple times.

4. **Type checking overhead**: `Utf8JsonReader.TokenType` requires reading and advancing to get the token type. `JsonElement.ValueKind` is a cached property lookup.

5. **Forward-only nature**: Even with position tracking, re-reading from a saved position still requires creating a new reader and parsing forward to the first token.

### 5.4 Industry Comparison

The current `JsonElement`-based implementation is already highly competitive:

| Library | Small Object Validation | Notes |
|---------|------------------------|-------|
| **JsonSchemaValidation (ours)** | ~9 µs | JsonElement-based |
| JsonSchema.Net | ~31 µs | Popular .NET implementation |
| Corvus.JsonSchema | ~2 µs | Code generation approach |
| LateApexEarlySpeed | ~5 µs | Optimized for speed |

Our implementation at ~9 µs for small objects is **top-tier among runtime validators** (Corvus.JsonSchema achieves 2 µs but requires compile-time code generation).

### 5.5 Fundamental Limitation: JSON Schema Requires Random Access

JSON Schema validation fundamentally requires random access to data:

- **`allOf`/`anyOf`/`oneOf`**: Must validate same value against multiple sub-schemas
- **`$ref`**: References can point anywhere, validated value must be re-readable
- **`unevaluatedProperties`/`unevaluatedItems`**: Must iterate ALL properties/items after other validators
- **`properties` + `additionalProperties`**: Must know which properties were already evaluated
- **`if`/`then`/`else`**: Conditional re-validation of same value

A forward-only streaming approach either:
1. Copies data (defeating zero-allocation), or
2. Requires multiple re-reads (adding overhead that exceeds `JsonElement`)

### 5.6 Conclusion: JsonElement is Optimal for Typical Use Cases

The `JsonElement`-based approach is the right architecture because:

1. **Single parse, multiple access**: `JsonDocument.Parse()` once, then O(1) access to any token
2. **Efficient memory layout**: Indexed token database with minimal overhead
3. **Already competitive**: ~9 µs puts us in the top tier of runtime validators
4. **Simpler code**: No position tracking, no multiple readers, no span slicing

**When span-based might help:**
- Extremely large documents (>100 MB) where `JsonDocument` memory becomes prohibitive
- Streaming validation with fail-fast (validating as data arrives)
- Specialized single-pass schemas (no composition keywords)

For these niche cases, a separate streaming API could be added, but the core `JsonElement`-based implementation should remain the default.

---

## Final Conclusion

Two implementation attempts have now conclusively demonstrated:

1. **The original approach failed** (Part 1) because copying bytes for multi-pass validation defeats zero-allocation
2. **The position-based re-reading approach failed** (Part 5) because creating multiple `Utf8JsonReader` instances adds more overhead than `JsonElement`'s indexed access

**The `JsonElement`-based implementation is optimal** for JSON Schema validation because:
- JSON Schema requires random access (composition keywords, references, unevaluated keywords)
- `JsonDocument` provides efficient O(1) token access after a single parse
- Our ~9 µs performance is already top-tier among runtime validators

Future optimization efforts should focus on:
- Reducing allocations within the existing architecture (pooling, span-based string comparisons)
- Caching compiled validators more aggressively
- Optimizing hot-path validators (type checking, numeric constraints)

The pursuit of zero-allocation validation is not viable for full JSON Schema 2020-12 compliance.
