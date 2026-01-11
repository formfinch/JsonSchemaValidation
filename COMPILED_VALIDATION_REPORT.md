# Compiled Validation POC Report

## Summary

**Status: ABANDONED** - The expression tree compilation approach does not provide performance benefits and actually degrades performance.

## Objective

Investigate whether using .NET expression trees to compile validators at schema load time could improve validation throughput, similar to how Ajv compiles schemas to JavaScript functions.

## Approach

Created compiled versions of 17 keyword validators using `System.Linq.Expressions`:

### Validators Implemented
- **Type validators**: string, number, boolean, array, object, null (with singleton caching)
- **Numeric validators**: Minimum, Maximum, ExclusiveMinimum, ExclusiveMaximum, MultipleOf
- **String validators**: MinLength, MaxLength, Pattern
- **Array/Object validators**: MinItems, MaxItems, MinProperties, MaxProperties
- **Other validators**: Required, Const

### Architecture
- Each compiled validator implements both `IKeywordValidator` and `ICompiledValidator`
- Validators build expression trees in their constructors
- Expression trees are compiled to `Func<JsonElement, bool>` delegates
- Type validators use `Lazy<T>` singletons to avoid repeated compilation

## Benchmark Results

| Metric | Non-Compiled | Compiled | Difference |
|--------|--------------|----------|------------|
| Median time | 9.1 µs | 14.9 µs | **63% slower** |
| Throughput | 110K/s | 67K/s | **39% lower** |
| Memory | 457.5 KB | 468.5 KB | slightly worse |

## Analysis

### Why Compiled Validators Are Slower

1. **Compilation Overhead**: `Expression.Lambda<T>().Compile()` involves JIT compilation, which is expensive. Each validator instance compiles its own expression tree during construction.

2. **Per-Keyword Compilation**: Unlike Ajv which compiles an entire schema into a single optimized function, this approach compiles each keyword separately. A schema with 10 keywords compiles 10 separate expression trees.

3. **Delegate Invocation Overhead**: Even after compilation, delegate invocation has overhead (~10-20ns per call). The original validators' direct method calls are already efficient.

4. **No Cross-Keyword Optimization**: Compiled delegates cannot be combined or inlined. Each validation still goes through the validator loop with separate delegate calls.

### Comparison with Ajv

Ajv achieves 35x faster validation than JSV because:
- Compiles entire schema to single JavaScript function
- V8 JIT optimizes the generated code holistically
- No per-keyword dispatch overhead
- Schema structure is "baked in" at compile time

Our approach only eliminates virtual dispatch for individual keywords, which is a tiny fraction of total validation time.

## What Would Be Required for Real Improvement

To match Ajv's approach, we would need to:

1. **Whole-Schema Compilation**: Generate a single delegate that handles all validation logic for a schema, including nested subschemas.

2. **Eliminate Context Allocation**: The `IJsonValidationContext` allocation per validation call is a significant cost.

3. **Inline Subschema Validation**: `$ref`, `allOf`, `anyOf`, etc. would need to inline their target schemas rather than call separate validators.

4. **Skip Location Tracking**: Error location tracking (`instanceLocation`, `keywordLocation`) adds overhead that could be skipped for boolean-only validation.

This would be a major architectural change affecting the entire validation pipeline.

## Recommendation

**Do not pursue compiled validators further** with the current per-keyword approach. The overhead exceeds any benefit.

If performance parity with Ajv is critical, consider:
1. A complete rewrite with whole-schema compilation
2. Source generators to generate validation code at compile time
3. Accept that .NET will be slower than V8-optimized JavaScript for this workload

## Files Created (Not Committed)

- `JsonSchemaValidation/Abstractions/Keywords/ICompiledValidator.cs`
- `JsonSchemaValidation/Draft202012/Keywords/Compiled/*.cs`
- `JsonSchemaValidation/Validation/CompiledSchemaValidator.cs`

These files were removed as part of reverting this POC.
