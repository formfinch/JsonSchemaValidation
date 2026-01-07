# Parallel Validation Analysis Report

## Executive Summary

An analysis was conducted to determine whether internal parallelization of JSON Schema validation could provide significant performance improvements. The conclusion is that **parallelization is not recommended** for the current implementation due to architectural barriers and limited benefit for typical workloads.

The thread-safety improvements already implemented (January 2026) ensure the library is safe for concurrent external usage—multiple threads validating different documents simultaneously. This is sufficient for most real-world scenarios.

---

## Part 1: Parallelization Opportunities Identified

### 1.1 High-Value Candidates

| Component | Current | Opportunity | Benefit | Complexity |
|-----------|---------|-------------|---------|------------|
| **ContainsValidator** | Sequential | HIGHEST | Large arrays | Low |
| **OneOfValidator** | Sequential | HIGH | 3+ subschemas | High |
| **AnyOfValidator** | Sequential | HIGH | 3+ subschemas | High |
| **AllOfValidator** | Sequential | HIGH | 3+ subschemas | High |
| **ItemsValidator** | Sequential | MEDIUM | Large arrays | Medium |

### 1.2 Why These Are Candidates

**ContainsValidator** (`Draft202012/Keywords/Array/ContainsValidator.cs`):
- Must validate ALL array items (no early exit)
- Collects indices of matching items
- Items are independent—no data dependencies
- Perfect candidate: straightforward parallelization

**Applicator Validators** (AllOf, AnyOf, OneOf):
- All subschemas must be validated regardless of individual results
- Each subschema receives a fresh context via `CreateFreshContext()`
- Results are aggregated after all complete
- Near-linear speedup potential with multiple cores

**ItemsValidator** (`Draft202012/Keywords/Array/ItemsValidator.cs`):
- Large arrays could benefit from parallel item validation
- However, has early-exit pattern (returns on first invalid item)
- Would require separate code paths for parallel vs sequential

### 1.3 Low-Value Candidates (Not Recommended)

| Component | Reason |
|-----------|--------|
| PrefixItemsValidator | Small fixed count (bounded by schema), early exit |
| PropertiesValidator | Typical object has few properties, mutable annotation state |
| PatternPropertiesValidator | Early exit pattern, nested loops |
| AdditionalPropertiesValidator | Early exit pattern |
| Keyword-level parallelism | Execution order constraints, scope sharing |

---

## Part 2: Critical Blockers

### 2.1 ValidationScope is Shared and Not Thread-Safe

**Location:** `Common/ValidationScope.cs`

```csharp
public class ValidationScope : IValidationScope
{
    private readonly Stack<SchemaMetadata> _schemaStack = new();

    public void PushSchemaResource(SchemaMetadata schema)
    {
        _schemaStack.Push(schema);  // NOT thread-safe
    }

    public void PopSchemaResource()
    {
        _schemaStack.Pop();  // NOT thread-safe
    }
}
```

**Problem:** The scope stack is used by `$ref` and `$dynamicRef` validators. Parallel execution of validators that modify scope would corrupt the stack.

**Example Race Condition:**
```
Thread 1: Push Schema A     Thread 2: Push Schema B
Thread 1: Validate          Thread 2: Validate
Thread 1: Pop             ← Thread 2 pops Schema A instead of B (corruption)
```

**Impact:** Blocks parallelization of applicator validators (AllOf, AnyOf, OneOf) and any nested validation that uses dynamic references.

### 2.2 Context Annotations Are Mutable and Not Thread-Safe

**Location:** `Common/JsonValidationArrayContext.cs`, `Common/JsonValidationObjectContext.cs`

**Array Context:**
```csharp
public struct Annotations
{
    public int EvaluatedIndex = -1;              // Mutable
    public HashSet<int> EvaluatedIndices = new();  // NOT thread-safe
}

public void SetEvaluatedIndex(int itemIndex)
{
    if (_current.EvaluatedIndex < itemIndex)
        _current.EvaluatedIndex = itemIndex;  // Race condition
}

public void SetEvaluatedIndices(IEnumerable<int> indices)
{
    foreach (int idx in indices)
        _current.EvaluatedIndices.Add(idx);  // HashSet.Add NOT thread-safe
}
```

**Object Context:**
```csharp
public struct Annotations
{
    public Dictionary<string, JsonProperty> UnEvaluatedProperties = new();
}

public void MarkPropertyEvaluated(string propertyName)
{
    _current.UnEvaluatedProperties.Remove(propertyName);  // NOT thread-safe
}
```

**Impact:** Blocks parallelization of array item validation and object property validation.

### 2.3 Early Termination Patterns

Many validators return immediately on first invalid result:

```csharp
// ItemsValidator.cs lines 44-60
foreach (JsonElement item in context.Data.EnumerateArray())
{
    // ... validation ...
    if (!itemValidationResult.IsValid)
    {
        return ValidationResult.Invalid(...);  // Early exit
    }
}
```

**Impact:** Early termination is incompatible with parallelization—you must validate all items before knowing if any failed. This means:
- Parallel validation would do MORE work for invalid inputs
- Separate code paths needed for parallel vs sequential
- Configuration required to choose behavior

---

## Part 3: What Would Be Required

### 3.1 Scope Isolation

**Option A: ThreadLocal Scope (Recommended)**
```csharp
public class ValidationScope : IValidationScope
{
    private static readonly ThreadLocal<Stack<SchemaMetadata>> _schemaStack =
        new(() => new Stack<SchemaMetadata>());

    public void PushSchemaResource(SchemaMetadata schema)
        => _schemaStack.Value!.Push(schema);

    public void PopSchemaResource()
        => _schemaStack.Value!.Pop();
}
```

**Option B: Scope Copying**
```csharp
// Create isolated scope for each parallel branch
var parallelScopes = _validators.Select(_ => context.Scope.Clone()).ToList();
```

**Concerns:**
- ThreadLocal adds overhead for non-parallel paths
- Scope copying is expensive for deep stacks
- `$dynamicRef` resolution depends on scope state—isolation may break semantics

### 3.2 Thread-Safe Annotation Collection

**Option A: ConcurrentCollections**
```csharp
public struct Annotations
{
    public ConcurrentBag<int> EvaluatedIndices = new();
}
```

**Option B: Per-Task Collection with Merge**
```csharp
// Each parallel task collects independently
var taskResults = await Task.WhenAll(tasks);

// Merge after all complete
foreach (var result in taskResults)
    context.MergeAnnotations(result.Annotations);
```

### 3.3 Result Ordering

Parallel tasks complete in arbitrary order. Results must be reordered for consistent output:

```csharp
var results = new ConcurrentDictionary<int, ValidationResult>();
Parallel.For(0, count, idx => {
    results[idx] = Validate(items[idx]);
});

// Reconstruct ordered list
var children = Enumerable.Range(0, count)
    .Select(i => results[i])
    .ToList();
```

---

## Part 4: Performance Analysis

### 4.1 Overhead vs Benefit

| Document Size | Sequential Cost | Parallel Overhead | Net Benefit |
|---------------|-----------------|-------------------|-------------|
| Small (< 10 items) | ~1-5 µs | ~10-50 µs | **Negative** |
| Medium (10-50 items) | ~5-50 µs | ~10-50 µs | **Marginal** |
| Large (100+ items) | ~100+ µs | ~10-50 µs | **Positive** |
| Very Large (1000+ items) | ~1+ ms | ~10-50 µs | **Significant** |

**Overhead sources:**
- Task creation and scheduling: ~1-5 µs per task
- Thread synchronization: ~1-10 µs per synchronization point
- Result collection and ordering: O(n) additional work
- Memory allocation for concurrent collections

### 4.2 Breakeven Points (Estimated)

| Scenario | Breakeven |
|----------|-----------|
| Array item validation | ~50-100 items |
| Applicator subschemas | ~3-4 subschemas |
| Object properties | ~50+ properties with expensive nested schemas |

### 4.3 Real-World Considerations

Most JSON Schema validation workloads involve:
- Small to medium documents (< 100 properties/items)
- Simple schemas (< 5 subschemas in applicators)
- High throughput requirements (many documents, not large documents)

For these typical workloads, **document-level parallelism** is more effective:

```csharp
// Already works safely after thread-safety fixes
var documents = GetJsonDocuments();  // Many small documents
var results = await Task.WhenAll(documents.Select(doc => Task.Run(() =>
{
    var context = contextFactory.CreateContextForRoot(doc);
    return validator.ValidateRoot(context);
})));
```

---

## Part 5: Keyword-Level Parallelism Analysis

### 5.1 Why Not Run Same-ExecutionOrder Validators in Parallel?

Validators are sorted by `ExecutionOrder` (default 0, unevaluated keywords use 100):

```csharp
// SchemaDraft202012ValidatorFactory.cs line 18
_keywordFactories = keywordFactories.OrderBy(f => f.ExecutionOrder).ToList();
```

**Theoretical approach:**
```csharp
foreach (var group in _keywordValidators.GroupBy(v => v.ExecutionOrder))
{
    var tasks = group.Select(v => Task.Run(() => v.Validate(context)));
    await Task.WhenAll(tasks);
}
```

**Why this doesn't work:**

1. **Scope Sharing:** `$ref` and `$dynamicRef` validators (ExecutionOrder=0) push/pop the shared scope. Running them in parallel with other validators corrupts scope.

2. **Nested Validation:** Even "simple" validators like `PropertiesValidator` recursively validate nested schemas, which may contain `$dynamicRef`:
   ```
   PropertiesValidator
   └── Nested SchemaValidator
       └── DynamicRefValidator  ← Pushes/pops shared scope
   ```

3. **Limited Benefit:** Most schemas have 5-15 keyword validators at ExecutionOrder=0. Parallelization overhead exceeds benefit.

4. **Complexity:** Would require scope isolation per validator, result synchronization, and execution order group management.

---

## Part 6: Configuration Design (If Implemented)

If parallelization is pursued in the future, it should be opt-in:

```csharp
public class SchemaValidationOptions
{
    public bool EnableDraft202012 { get; set; }
    public bool FormatAssertionEnabled { get; set; }

    // Parallelization options
    public ParallelValidationOptions ParallelValidation { get; set; } = new();
}

public class ParallelValidationOptions
{
    /// <summary>
    /// Enable parallel validation for large arrays and applicators.
    /// Default: false (sequential validation)
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Minimum array size to trigger parallel item validation.
    /// Default: 100 items
    /// </summary>
    public int ArrayItemThreshold { get; set; } = 100;

    /// <summary>
    /// Minimum subschema count to trigger parallel applicator validation.
    /// Default: 4 subschemas
    /// </summary>
    public int ApplicatorThreshold { get; set; } = 4;

    /// <summary>
    /// Maximum degree of parallelism.
    /// Default: Environment.ProcessorCount
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}
```

**Rationale for opt-in:**
1. Most validation is fast without parallelization
2. Overhead dominates for small inputs
3. Thread pool contention in web applications
4. Predictable latency is often more important than throughput

---

## Part 7: Implementation Roadmap (If Pursued)

### Phase 1: Low-Risk, High-ROI
**ContainsValidator parallelization**
- No early termination
- No scope interaction
- Straightforward result aggregation
- Estimated effort: 1-2 days

### Phase 2: High-Value, High-Complexity
**Applicator parallelization (AllOf, AnyOf, OneOf)**
- Requires scope isolation solution
- Fresh contexts already created
- Near-linear speedup potential
- Estimated effort: 3-5 days

### Phase 3: Medium-Value
**ItemsValidator parallelization**
- Requires separate code path (skip early exit)
- Context annotation synchronization needed
- Estimated effort: 2-3 days

---

## Part 8: Conclusion and Recommendation

### 8.1 Recommendation: Do Not Implement Internal Parallelization

**Reasons:**

1. **Architectural barriers are significant**
   - ValidationScope sharing requires redesign
   - Context mutations require thread-safe collections or isolation
   - Early termination patterns incompatible with parallelization

2. **Benefit is limited for typical workloads**
   - Most JSON documents are small
   - Most schemas have few applicator subschemas
   - Overhead often exceeds benefit

3. **Document-level parallelism is sufficient**
   - Thread-safety fixes already enable safe concurrent usage
   - Users can parallelize at the document level
   - Simpler, more predictable performance

4. **Complexity and maintenance burden**
   - Two code paths (parallel and sequential)
   - Configuration complexity
   - Testing complexity (race conditions)

### 8.2 Current State is Optimal for Most Use Cases

The thread-safety improvements completed in January 2026 ensure:
- Multiple threads can validate different documents concurrently
- Schema registration is thread-safe
- Service initialization is thread-safe

This is sufficient for:
- Web applications validating request bodies
- Batch processing of multiple JSON files
- Microservices with concurrent requests

### 8.3 When to Reconsider

Internal parallelization should only be reconsidered if:
1. A concrete use case demonstrates need (e.g., validating 10,000+ item arrays)
2. Benchmarks show >50% improvement for that use case
3. Resources available for proper implementation and testing

---

## Appendix A: Thread-Safety Fixes Completed

The following thread-safety issues were fixed (January 2026):

### A.1 SchemaDraft202012Meta._schemas
**Problem:** Static `List<JsonElement>` with race condition during initialization
**Solution:** `Lazy<IReadOnlyList<JsonElement>>` for thread-safe one-time initialization

### A.2 SchemaRepository._sortedSchemas
**Problem:** Unprotected read-write race between `AddSchema()` and `TryGetDynamicRef()`
**Solution:** `volatile` field with immutable snapshot (`IReadOnlyList<T>`)

### A.3 LazySchemaValidatorFactory.Value
**Problem:** Non-atomic `??=` assignment
**Solution:** `Interlocked.CompareExchange` for atomic compare-and-swap

### A.4 Regression Tests
**Location:** `JsonSchemaValidationTests/Draft202012/ThreadSafety/RegressionTests.cs`
- Concurrent validation tests (100 parallel validations)
- Concurrent schema registration tests
- Concurrent initialization tests
- Stress tests (500 parallel validations)
- Deadlock detection tests

---

## Appendix B: Files Analyzed

| File | Purpose | Parallelization Potential |
|------|---------|---------------------------|
| `Validation/SchemaValidator.cs` | Keyword orchestration | Low (execution order) |
| `Draft202012/Keywords/Array/ItemsValidator.cs` | Array item validation | Medium (large arrays) |
| `Draft202012/Keywords/Array/ContainsValidator.cs` | Contains validation | High (no early exit) |
| `Draft202012/Keywords/Array/PrefixItemsValidator.cs` | Prefix items | Low (small count) |
| `Draft202012/Keywords/Logic/AllOfValidator.cs` | All-of applicator | High (with scope fix) |
| `Draft202012/Keywords/Logic/AnyOfValidator.cs` | Any-of applicator | High (with scope fix) |
| `Draft202012/Keywords/Logic/OneOfValidator.cs` | One-of applicator | High (with scope fix) |
| `Draft202012/Keywords/Object/PropertiesValidator.cs` | Property validation | Medium |
| `Common/ValidationScope.cs` | Dynamic scope tracking | Blocker (not thread-safe) |
| `Common/JsonValidationArrayContext.cs` | Array annotations | Blocker (mutable state) |
| `Common/JsonValidationObjectContext.cs` | Object annotations | Blocker (mutable state) |
| `Validation/ValidationResult.cs` | Result aggregation | Safe (immutable record) |
