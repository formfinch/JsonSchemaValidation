# TASK-048: Implement Scope Stack for $dynamicRef in Compiled Validators

## Implementation Plan

**Status:** COMPLETE (2026-01-28)
**Priority:** Medium
**Estimated Complexity:** High

> **Implementation Summary:**
> Full $dynamicRef support implemented in compiled validators including:
> - Scope stack infrastructure with outermost-first anchor resolution
> - Resource-level anchor collection for nested `$defs`
> - Cross-resource `$dynamicRef` support (e.g., `"extended#meta"`)
> - Fragment reference scope pushing (entering resource via `#/$defs/foo`)
> - Evaluated state tracking for `unevaluatedProperties`/`unevaluatedItems` across external refs
>
> **Results:** 3971 passed, 192 skipped, 0 failed (+10 tests from before implementation)
> All previously skipped $dynamicRef tests now pass.

---

## Problem Statement

Compiled validators currently cannot properly resolve `$dynamicRef` (Draft 2020-12) and `$recursiveRef` (Draft 2019-09) when they require runtime scope inspection.

### Current Behavior

The current implementation stores only a single `_dynamicScopeRoot` field:

```csharp
private ICompiledValidator? _dynamicScopeRoot;

// Generated $dynamicRef code:
if (_dynamicScopeRoot != null)
{
    if (!_dynamicScopeRoot.IsValid(e)) return false;
}
else
{
    if (!Validate_LocalAnchor(e)) return false;
}
```

This fails when there are **multiple levels** in the dynamic scope chain.

### Expected Behavior (Per Spec)

When a schema with `$dynamicRef: "#anchorName"` is evaluated:
1. Check if the **static target** has a matching `$dynamicAnchor` (bookend check)
2. If NO bookend, behave as normal `$ref` (no dynamic resolution)
3. If bookend exists, search the dynamic scope stack from **outermost to innermost**
4. Return the **first** matching `$dynamicAnchor` found
5. If no match in dynamic scope, fall back to the static target

---

## Specification Citations

### $dynamicRef (Draft 2020-12)

From [JSON Schema Core 2020-12, Section 8.2.3.2](https://json-schema.org/draft/2020-12/json-schema-core):

> "If the initially resolved starting point URI includes a fragment that was created by the `$dynamicAnchor` keyword, the initial URI MUST be replaced by the URI (including the fragment) for the **outermost schema resource in the dynamic scope** that defines an identically named fragment with `$dynamicAnchor`."

### $recursiveRef (Draft 2019-09)

From [JSON Schema Core 2019-09](https://json-schema.org/draft/2019-09/draft-handrews-json-schema-02) and [Learn JSON Schema](https://www.learnjsonschema.com/2019-09/core/recursiveref/):

> "If `$recursiveAnchor` is set to true, then when the containing schema object is used as a target of `$recursiveRef`, a new base URI is determined by examining the dynamic scope for the **outermost** schema that also contains `$recursiveAnchor` with a value of true."

**Both specs explicitly state: outermost first.**

---

## Test Case Evidence

### $dynamicRef Order Test (`dynamicRef.json:296-346`)

This test proves that outermost anchor takes precedence:

```json
{
  "$id": "root",
  "$dynamicAnchor": "meta",
  "properties": { "foo": { "const": "pass" } },  // ROOT requires foo="pass"
  "$ref": "extended",
  "$defs": {
    "extended": {
      "$id": "extended",
      "$dynamicAnchor": "meta",  // EXTENDED has no foo constraint
      "properties": { "bar": { "$ref": "bar" } }
    },
    "bar": {
      "properties": { "baz": { "$dynamicRef": "extended#meta" } }
    }
  }
}
```

**Test results:**
- `{ "foo": "pass", "bar": { "baz": { "foo": "pass" } } }` → **VALID** (nested validates against ROOT)
- `{ "foo": "pass", "bar": { "baz": { "foo": "fail" } } }` → **INVALID** (nested fails ROOT's constraint)

If innermost won, the second case would be valid (extended has no foo constraint). The test proves outermost wins.

### $recursiveRef Order Test (`recursiveRef.json:34-135`)

Comparing two schema variations:

**Without outer $recursiveAnchor (lines 34-83):**
- Only `myobject.json` has `$recursiveAnchor: true`
- `{ "foo": 1 }` → **INVALID** (recursiveRef stays within myobject, which only allows string/object)

**With outer $recursiveAnchor (lines 85-135):**
- Both root AND myobject have `$recursiveAnchor: true`
- `{ "foo": 1 }` → **VALID** (recursiveRef resolves to outermost root, which allows integer)

This proves outermost-first resolution.

---

## Design Decisions

### API Approach: New Interface

Add `IScopedCompiledValidator` extending `ICompiledValidator`:

```csharp
public interface IScopedCompiledValidator : ICompiledValidator
{
    /// <summary>
    /// Dynamic anchors declared by this validator.
    /// Key: anchor name (without #)
    /// Value: validation function (element, scope) -> bool
    /// </summary>
    IReadOnlyDictionary<string, Func<JsonElement, ICompiledValidatorScope, bool>>? DynamicAnchors { get; }

    /// <summary>
    /// True if this validator has $recursiveAnchor: true.
    /// </summary>
    bool HasRecursiveAnchor { get; }

    /// <summary>
    /// Root validation function for $recursiveRef resolution.
    /// </summary>
    Func<JsonElement, ICompiledValidatorScope, bool>? RootValidator { get; }

    /// <summary>
    /// Validates with dynamic scope tracking.
    /// </summary>
    bool IsValid(JsonElement instance, ICompiledValidatorScope scope);
}
```

### Scope Push Rules (Clarified)

**Push when entering a schema that declares anchors:**
- Has `$dynamicAnchor` declaration(s), OR
- Has `$recursiveAnchor: true`

**Do NOT push for:**
- Schema with only `$id` (no anchors) - `$id` alone doesn't contribute to dynamic resolution
- Subschemas without anchors
- Intermediate applicators (allOf, anyOf, etc.)

**Resolved conflict:** The earlier "push on $id" was incorrect. Only anchors matter for scope resolution. Schemas with `$id` but no anchors don't need to be in the scope stack.

### Bookend Check in Generated Code

The generated code MUST check for bookend at compile time or generate runtime guard:

```csharp
// Generated code for $dynamicRef: "target#anchorName"
// COMPILE-TIME: Code generator checks if static target has matching $dynamicAnchor
// If no bookend exists, generate normal $ref code instead

// If bookend exists, generate:
if (scope.TryResolveDynamicAnchor("anchorName", out var dynamicValidator))
{
    if (!dynamicValidator!(e, scope)) return false;
}
else
{
    // Fall back to static target (the bookend)
    if (!Validate_StaticTarget(e, scope)) return false;
}
```

The code generator already performs this check - see `DynamicRefCodeGenerator.cs:FindDynamicAnchorInResource()`.

### Scope Semantics When Calling Resolved Anchor

**Question:** When we find an anchor at an outer scope and call it, do we pass the current (full) scope or truncate to that anchor's scope?

**Answer:** Pass the **full current scope**.

**Rationale:** The resolved anchor's validation may traverse to other schemas that themselves use `$dynamicRef`. Those traversals should see the complete dynamic scope, not a truncated one. The spec says "outermost in the dynamic scope" - the dynamic scope is the full evaluation path, which remains intact.

Example: If root → A → B → $dynamicRef resolves to root, root's validation might traverse to C which has its own $dynamicRef. C's resolution should still see A and B in scope.

### Non-Scoped Validator Participation

All FormFinch-generated validators implement `IScopedCompiledValidator`. For external validators:

```csharp
if (externalValidator is IScopedCompiledValidator scoped)
{
    return scoped.IsValid(element, currentScope);
}
else
{
    // Graceful degradation - validate without scope participation
    return externalValidator.IsValid(element);
}
```

### Backward Compatibility: IsValid(JsonElement) Wrapper

```csharp
public bool IsValid(JsonElement instance)
{
    // Create scope with self as the outermost entry (if we have anchors)
    var initialScope = CompiledValidatorScope.Empty;
    if (DynamicAnchors?.Count > 0 || HasRecursiveAnchor)
    {
        initialScope = initialScope.Push(new CompiledScopeEntry
        {
            DynamicAnchors = DynamicAnchors,
            RootValidator = RootValidator,
            HasRecursiveAnchor = HasRecursiveAnchor
        });
    }
    return IsValid(instance, initialScope);
}
```

---

## Scope Resolution API

### ICompiledValidatorScope Interface

```csharp
public interface ICompiledValidatorScope
{
    /// <summary>
    /// Searches from outermost to innermost for matching $dynamicAnchor.
    /// </summary>
    bool TryResolveDynamicAnchor(
        string anchorName,
        out Func<JsonElement, ICompiledValidatorScope, bool>? validator);

    /// <summary>
    /// Searches from outermost to innermost for $recursiveAnchor: true.
    /// </summary>
    bool TryResolveRecursiveAnchor(
        out Func<JsonElement, ICompiledValidatorScope, bool>? validator);

    /// <summary>
    /// Creates a new scope with entry pushed.
    /// Returns this instance unchanged if entry.HasAnyAnchors is false.
    /// </summary>
    ICompiledValidatorScope Push(CompiledScopeEntry entry);

    bool IsEmpty { get; }
}
```

### CompiledScopeEntry Struct (Aligned Definition)

```csharp
/// <summary>
/// Entry in the compiled validator scope stack.
/// </summary>
public readonly struct CompiledScopeEntry
{
    /// <summary>
    /// Dynamic anchors declared at this scope level.
    /// Key: anchor name (without #)
    /// Value: validation function (element, scope) -> bool
    /// Null if no dynamic anchors.
    /// </summary>
    public IReadOnlyDictionary<string, Func<JsonElement, ICompiledValidatorScope, bool>>? DynamicAnchors { get; init; }

    /// <summary>
    /// Root validation function for $recursiveRef resolution.
    /// Null if HasRecursiveAnchor is false.
    /// </summary>
    public Func<JsonElement, ICompiledValidatorScope, bool>? RootValidator { get; init; }

    /// <summary>
    /// True if $recursiveAnchor: true is declared.
    /// </summary>
    public bool HasRecursiveAnchor { get; init; }

    /// <summary>
    /// True if this entry contributes to scope resolution.
    /// </summary>
    public bool HasAnyAnchors => DynamicAnchors?.Count > 0 || HasRecursiveAnchor;
}
```

---

## Implementation

### CompiledValidatorScope (Performance Strategy: stackalloc)

Single strategy chosen: **stackalloc for depth ≤ 8, heap array for deeper**.

```csharp
public sealed class CompiledValidatorScope : ICompiledValidatorScope
{
    public static readonly CompiledValidatorScope Empty = new(null, default);

    private readonly CompiledValidatorScope? _parent;
    private readonly CompiledScopeEntry _entry;

    private CompiledValidatorScope(CompiledValidatorScope? parent, CompiledScopeEntry entry)
    {
        _parent = parent;
        _entry = entry;
    }

    public bool IsEmpty => _parent == null && !_entry.HasAnyAnchors;

    public bool TryResolveDynamicAnchor(
        string anchorName,
        out Func<JsonElement, ICompiledValidatorScope, bool>? validator)
    {
        // Count depth
        var depth = 0;
        var current = this;
        while (current != null && (current._parent != null || current._entry.HasAnyAnchors))
        {
            depth++;
            current = current._parent;
        }

        if (depth == 0)
        {
            validator = null;
            return false;
        }

        // Build path array (stackalloc for common case)
        Span<CompiledValidatorScope> path = depth <= 8
            ? stackalloc CompiledValidatorScope[depth]
            : new CompiledValidatorScope[depth];

        current = this;
        for (int i = depth - 1; i >= 0 && current != null; i--)
        {
            path[i] = current;
            current = current._parent;
        }

        // Search outermost (index 0) to innermost
        for (int i = 0; i < depth; i++)
        {
            if (path[i]._entry.DynamicAnchors?.TryGetValue(anchorName, out validator) == true)
            {
                return true;
            }
        }

        validator = null;
        return false;
    }

    public bool TryResolveRecursiveAnchor(
        out Func<JsonElement, ICompiledValidatorScope, bool>? validator)
    {
        // Same traversal - outermost first
        var depth = 0;
        var current = this;
        while (current != null && (current._parent != null || current._entry.HasAnyAnchors))
        {
            depth++;
            current = current._parent;
        }

        if (depth == 0)
        {
            validator = null;
            return false;
        }

        Span<CompiledValidatorScope> path = depth <= 8
            ? stackalloc CompiledValidatorScope[depth]
            : new CompiledValidatorScope[depth];

        current = this;
        for (int i = depth - 1; i >= 0 && current != null; i--)
        {
            path[i] = current;
            current = current._parent;
        }

        for (int i = 0; i < depth; i++)
        {
            if (path[i]._entry.HasRecursiveAnchor && path[i]._entry.RootValidator != null)
            {
                validator = path[i]._entry.RootValidator;
                return true;
            }
        }

        validator = null;
        return false;
    }

    public ICompiledValidatorScope Push(CompiledScopeEntry entry)
    {
        // Optimization: skip if entry has nothing to contribute
        if (!entry.HasAnyAnchors)
        {
            return this;
        }
        return new CompiledValidatorScope(this, entry);
    }
}
```

**Performance fallback:** If benchmarks show `TryResolveDynamicAnchor` is a bottleneck (>5% of validation time), consider:
1. Caching the path array in the scope instance
2. Using a flat array-based scope instead of linked list

---

## Code Generator Updates

### Mechanical Guard for Generator Completeness

**Problem:** Manual list of generators risks missing one.

**Solution:** Update the common validation call helper to require scope:

```csharp
// In CodeGenerationContext.cs
public string GenerateValidateCall(string hash)
{
    // OLD: return $"Validate_{hash}(e)";
    // NEW: Forces all generators to pass scope
    return $"Validate_{hash}(e, scope)";
}

public string GenerateExternalValidatorCall(string fieldName)
{
    return $"""
        if ({fieldName} is IScopedCompiledValidator {fieldName}Scoped)
            {{ if (!{fieldName}Scoped.IsValid(e, scope)) return false; }}
        else
            {{ if (!{fieldName}.IsValid(e)) return false; }}
        """;
}
```

Any generator that tries to call a validator without scope will fail to compile because the method signature requires it.

### Generators That Need Updates

All generators that call subvalidators will automatically get scope passing via `GenerateValidateCall()`:

| Generator | Uses GenerateValidateCall |
|-----------|---------------------------|
| RefCodeGenerator | Yes |
| DynamicRefCodeGenerator | Yes (+ TryResolve) |
| RecursiveRefCodeGenerator | Yes (+ TryResolve) |
| AllOfCodeGenerator | Yes |
| AnyOfCodeGenerator | Yes |
| OneOfCodeGenerator | Yes |
| NotCodeGenerator | Yes |
| IfThenElseCodeGenerator | Yes |
| PropertiesCodeGenerator | Yes |
| ItemsCodeGenerator | Yes |
| PrefixItemsCodeGenerator | Yes |
| ContainsCodeGenerator | Yes |
| AdditionalPropertiesCodeGenerator | Yes |
| UnevaluatedPropertiesCodeGenerator | Yes |
| UnevaluatedItemsCodeGenerator | Yes |
| PatternPropertiesCodeGenerator | Yes |
| DependentSchemasCodeGenerator | Yes |
| PropertyNamesCodeGenerator | Yes |

### DynamicRefCodeGenerator Update

```csharp
public string GenerateCode(CodeGenerationContext context)
{
    // ... parse $dynamicRef ...

    // BOOKEND CHECK (compile-time)
    var staticTarget = ResolveStaticTarget(context, refValue);
    if (!HasMatchingDynamicAnchor(staticTarget, anchorName))
    {
        // No bookend - generate normal $ref code
        return context.GenerateValidateCall(staticTargetHash);
    }

    // Has bookend - generate dynamic resolution code
    return $$"""
        if (scope.TryResolveDynamicAnchor("{{anchorName}}", out var _dyn_{{hash}}))
        {
            if (!_dyn_{{hash}}!(e, scope)) return false;
        }
        else
        {
            if (!{{context.GenerateValidateCall(staticTargetHash)}}) return false;
        }
        """;
}
```

---

## Testing

### Unit Tests: Scope Stack Mechanics

```csharp
[Fact]
public void EmptyScope_TryResolve_ReturnsFalse()
{
    Assert.False(CompiledValidatorScope.Empty.TryResolveDynamicAnchor("x", out _));
}

[Fact]
public void SingleEntry_MatchingAnchor_Found()
{
    var scope = CompiledValidatorScope.Empty.Push(new CompiledScopeEntry
    {
        DynamicAnchors = new Dictionary<string, ...> { ["items"] = (e, s) => true }
    });
    Assert.True(scope.TryResolveDynamicAnchor("items", out var validator));
    Assert.NotNull(validator);
}

[Fact]
public void MultipleEntries_OutermostWins()
{
    // Outer returns true, inner returns false
    var outer = CompiledValidatorScope.Empty.Push(new CompiledScopeEntry
    {
        DynamicAnchors = new Dictionary<string, ...> { ["items"] = (e, s) => true }
    });
    var inner = outer.Push(new CompiledScopeEntry
    {
        DynamicAnchors = new Dictionary<string, ...> { ["items"] = (e, s) => false }
    });

    // Should find outer's validator (returns true), not inner's
    Assert.True(inner.TryResolveDynamicAnchor("items", out var validator));
    Assert.True(validator!(default, inner)); // Proves it's outer's validator
}

[Fact]
public void PushWithNoAnchors_ReturnsSameInstance()
{
    var scope = CompiledValidatorScope.Empty;
    var result = scope.Push(new CompiledScopeEntry { HasRecursiveAnchor = false });
    Assert.Same(scope, result); // Optimization: no allocation
}
```

### Integration Tests: Full $dynamicRef Scenarios

Test cases from JSON-Schema-Test-Suite that MUST pass:

1. **Nested anchors** (`dynamicRef.json:296-346`) - outer anchor takes precedence
2. **Intermediate scopes** (`dynamicRef.json:158-...`) - scopes without matching anchor skipped
3. **No bookend fallback** (`dynamicRef.json:348-388`) - $anchor not $dynamicAnchor → behaves as $ref
4. **Multiple paths** (`dynamicRef.json:390-...`) - different paths through if/then/else
5. **Recursive ref with nesting** (`recursiveRef.json:85-135`) - outer $recursiveAnchor wins

---

## File Change Summary

### New Files (7)

| File | Purpose |
|------|---------|
| `Abstractions/ICompiledValidatorScope.cs` | Scope interface |
| `Abstractions/IScopedCompiledValidator.cs` | Extended validator interface |
| `CompiledValidators/CompiledScopeEntry.cs` | Scope entry struct |
| `CompiledValidators/CompiledValidatorScope.cs` | Implementation |
| `Tests/Compiler/CompiledScopeStackTests.cs` | Unit + integration tests |
| `docs/TASK-048-PERFORMANCE.md` | Performance comparison report |
| `benchmarks/` | Baseline and post-implementation benchmark data |

### Modified Files (~25)

| Category | Files | Changes |
|----------|-------|---------|
| Code generators | All that call validators | Use GenerateValidateCall (automatic scope) |
| CodeGenerationContext | 1 file | Add scope to GenerateValidateCall |
| SchemaCodeGenerator | 1 file | Generate IScopedCompiledValidator impl |
| Generated validators | 10+ files | Regenerated |
| Tests | 2 files | Remove skip conditions |

---

## Acceptance Criteria Checklist

### Phase 0: Baseline
- [ ] Baseline benchmarks recorded before any changes
- [ ] Baseline results saved to `benchmarks/baseline/`
- [ ] Git commit hash recorded

### Phase 1-4: Implementation
- [ ] Spec citations documented for resolution order
- [ ] Test cases identified that prove order
- [ ] `ICompiledValidatorScope` with `TryResolveDynamicAnchor` / `TryResolveRecursiveAnchor`
- [ ] `CompiledValidatorScope` with outermost-first search
- [ ] Scope push only when anchors exist (not just $id)
- [ ] Bookend check enforced in code generator
- [ ] `GenerateValidateCall` forces scope parameter (mechanical guard)
- [ ] `IsValid(JsonElement)` creates scope with self's anchors
- [ ] Full scope passed to resolved anchor validators
- [ ] All compiled validators regenerated
- [ ] Unit tests for scope mechanics (including order proof)
- [ ] Integration tests pass (dynamicRef.json, recursiveRef.json)

### Phase 5: Performance Validation
- [ ] Post-implementation benchmarks recorded
- [ ] Results saved to `benchmarks/with-scope-stack/`
- [ ] Performance comparison report created (`docs/TASK-048-PERFORMANCE.md`)
- [ ] Non-$dynamicRef scenarios: < 5% overhead
- [ ] $dynamicRef scenarios: < 20% overhead
- [ ] Performance impact clearly communicated with before/after data

---

## Implementation Order

1. **Phase 0** - Baseline performance recording (BEFORE any code changes)
2. **Phase 1** - Core infrastructure (interfaces and scope stack)
3. **Phase 2** - Code generator updates (GenerateValidateCall + specific generators)
4. **Phase 3** - Regenerate validators
5. **Phase 4** - Testing (unit + integration)
6. **Phase 5** - Performance comparison and reporting

---

## Performance Measurement Plan

### Phase 0: Baseline Recording (Before Changes)

**Critical:** Record baseline metrics BEFORE making any code changes.

Run the existing benchmark suite and capture results:

```bash
cd JsonSchemaValidation.Benchmarks
dotnet run -c Release -- --filter "*Compiled*" --exporters json md
```

**Metrics to capture:**

| Metric | Tool | Purpose |
|--------|------|---------|
| Validation throughput (ops/sec) | BenchmarkDotNet | Raw performance |
| Mean execution time (ns) | BenchmarkDotNet | Latency |
| Memory allocated (bytes) | BenchmarkDotNet | Allocation overhead |
| GC collections (Gen0/Gen1/Gen2) | BenchmarkDotNet | GC pressure |

**Scenarios to benchmark:**

| Scenario | Schema | Description |
|----------|--------|-------------|
| Simple validation | No $dynamicRef | Baseline for overhead measurement |
| Medium complexity | Properties + $ref | Typical usage pattern |
| Complex validation | Nested applicators | Stress test |
| Metaschema validation | Draft 2020-12 metaschema | Has $dynamicRef |

**Output files to preserve:**

```
benchmarks/baseline/
├── BenchmarkDotNet.Artifacts/
│   ├── results/
│   │   ├── *-report.json      # Machine-readable results
│   │   └── *-report.md        # Human-readable summary
├── baseline-summary.md         # Manual notes on test conditions
└── git-commit.txt              # Git commit hash at baseline
```

### Phase 5: Performance Comparison (After Changes)

**Run identical benchmarks after implementation:**

```bash
dotnet run -c Release -- --filter "*Compiled*" --exporters json md
```

**Save to separate directory:**

```
benchmarks/with-scope-stack/
├── BenchmarkDotNet.Artifacts/
│   └── results/
├── comparison-summary.md
└── git-commit.txt
```

### Performance Comparison Report

Create `docs/TASK-048-PERFORMANCE.md` with:

```markdown
# TASK-048 Performance Impact Report

## Test Environment
- CPU: [from BenchmarkDotNet]
- OS: [from BenchmarkDotNet]
- .NET: [version]
- Baseline commit: [hash]
- Implementation commit: [hash]

## Summary

| Scenario | Baseline | After | Delta | Verdict |
|----------|----------|-------|-------|---------|
| Simple (no $dynamicRef) | X ns | Y ns | +Z% | ✅/⚠️/❌ |
| Medium complexity | X ns | Y ns | +Z% | ✅/⚠️/❌ |
| Complex validation | X ns | Y ns | +Z% | ✅/⚠️/❌ |
| Metaschema ($dynamicRef) | X ns | Y ns | +Z% | ✅/⚠️/❌ |

## Verdict Criteria
- ✅ Pass: < 5% overhead for non-$dynamicRef scenarios
- ⚠️ Warning: 5-10% overhead (acceptable with justification)
- ❌ Fail: > 10% overhead for non-$dynamicRef scenarios

## Memory Impact

| Scenario | Baseline Alloc | After Alloc | Delta |
|----------|----------------|-------------|-------|
| Simple | X bytes | Y bytes | +Z bytes |
| With $dynamicRef | X bytes | Y bytes | +Z bytes |

## Detailed Results

[Link to full BenchmarkDotNet reports]

## Conclusions

[Analysis of results, whether targets were met, any optimization opportunities identified]
```

### Acceptance Thresholds

| Scenario | Max Acceptable Overhead |
|----------|------------------------|
| Validation without $dynamicRef | < 5% time, < 100 bytes/validation |
| Validation with $dynamicRef (shallow) | < 10% time |
| Validation with $dynamicRef (deep) | < 20% time |

### If Thresholds Exceeded

If performance targets are not met:

1. **Profile** - Use dotTrace or PerfView to identify hotspots
2. **Optimize** - Apply fallback strategies from plan:
   - Cache path array in scope instance
   - Switch to flat array-based scope
   - Add fast path for empty scope
3. **Re-benchmark** - Verify optimization effectiveness
4. **Document** - If overhead unavoidable, document trade-off (correctness vs performance)

---

## Sources

- [JSON Schema Core 2020-12](https://json-schema.org/draft/2020-12/json-schema-core) - $dynamicRef specification
- [JSON Schema Core 2019-09](https://json-schema.org/draft/2019-09/draft-handrews-json-schema-02) - $recursiveRef specification
- [Learn JSON Schema - $dynamicRef](https://www.learnjsonschema.com/2020-12/core/dynamicref/) - Educational reference
- [Learn JSON Schema - $recursiveRef](https://www.learnjsonschema.com/2019-09/core/recursiveref/) - Educational reference
- [JSON-Schema-Test-Suite](https://github.com/json-schema-org/JSON-Schema-Test-Suite) - Conformance tests
