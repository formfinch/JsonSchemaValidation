---
id: TASK-61
title: 'Implement multi-targeting for net6.0, net8.0, net10.0'
status: In Progress
assignee: []
created_date: '2026-01-31 23:13'
updated_date: '2026-02-02 21:10'
labels:
  - research
  - nuget
  - breaking-change
milestone: 'Phase 3: API Stability'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Implement multi-targeting support for `net6.0`, `net8.0`, and `net10.0` to maximize library adoption while maintaining performance on modern runtimes.

**Market Rationale:**
| Version | Share | Status |
|---------|-------|--------|
| .NET 8 | ~42% | LTS, active |
| .NET 10 | ~18% | LTS, active |
| .NET 6 | ~15% | EOL, but significant user base |

Targeting only net10.0 excludes ~60% of potential users.

**NuGet Package Structure:**
Multi-targeting produces separate assemblies per target, not one bulky combined assembly. NuGet automatically selects the appropriate DLL for the consumer's target framework.

**Compatibility Strategy:**
Create wrapper/passthrough types that encapsulate `#if` directives in one place (`Polyfills/` folder). This keeps the rest of the codebase clean and free of conditional compilation.

**Phased Implementation:**
1. **Phase A:** Add net8.0 alongside net10.0 (minimal changes - APIs are nearly identical)
2. **Phase B:** Add net6.0 alongside net8.0 and net10.0 (requires polyfills for DeepEquals, FrozenSet, Lock)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Project builds successfully for net6.0, net8.0, net10.0
- [ ] #2 All 2887 tests pass on all three targets
- [ ] #3 No new analyzer warnings
- [ ] #4 Compiled validators regenerated and working
- [ ] #5 CLAUDE.md updated with new target framework policy
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
## Phase A: Add net8.0 support (net8.0 + net10.0)

APIs are nearly identical between net8 and net10. Only the `Lock` type (net9+) needs replacing with `lock(object)`.

### A.1: Project Configuration
Update csproj files: `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`
- `JsonSchemaValidation/JsonSchemaValidation.csproj`
- `JsonSchemaValidation.CodeGeneration/JsonSchemaValidation.CodeGeneration.csproj`
- `JsonSchemaValidation.Compiler/JsonSchemaValidation.Compiler.csproj`
- `JsonSchemaValidationTests/JsonSchemaValidationTests.csproj`
- `JsonSchemaValidationTests.Stress/JsonSchemaValidationTests.Stress.csproj`
- `JsonSchemaValidation.Benchmarks/JsonSchemaValidation.Benchmarks.csproj`

### A.2: Replace Lock type with lock(object)
- Update `Common/LruCache.cs`: Replace `Lock` with `object`, use `lock(_lock)` pattern
- Update `Validation/SchemaValidator.cs`: Replace `Lock` with `object`, use `lock(_lock)` pattern

### A.3: Testing
```bash
dotnet build -c Release
dotnet test --framework net8.0
dotnet test --framework net10.0
```

---

## Phase B: Add net6.0 support (net6.0 + net8.0 + net10.0)

Requires polyfills for APIs not available in net6.

### B.1: Project Configuration
Update csproj files: `<TargetFrameworks>net6.0;net8.0;net10.0</TargetFrameworks>`

### B.2: Create Polyfills

**JsonElementHelper** (`Polyfills/JsonElementHelper.cs`)
- net8+: Delegates to `JsonElement.DeepEquals()`
- net6: Custom recursive implementation

**OptimizedSet<T>** (`Polyfills/OptimizedSet.cs`)
- net8+: Uses `FrozenSet<T>`
- net6: Uses `HashSet<T>`

**OptimizedDictionary<TKey, TValue>** (`Polyfills/OptimizedDictionary.cs`)
- net8+: Uses `FrozenDictionary<TKey, TValue>`
- net6: Uses `Dictionary<TKey, TValue>`

### B.3: Update Code Generator
- `ArrayConstraintsCodeGenerator.cs`: `JsonElement.DeepEquals` → `JsonElementHelper.DeepEquals`
- `EnumConstCodeGenerator.cs`: `JsonElement.DeepEquals` → `JsonElementHelper.DeepEquals`
- Add `--target-framework` CLI option

### B.4: Update Runtime Validators

**ConstValidator (all drafts):** `JsonElement.DeepEquals` → `JsonElementHelper.DeepEquals`
- Draft6, Draft7, Draft201909, Draft202012

**EnumValidator (all drafts):** `JsonElement.DeepEquals` → `JsonElementHelper.DeepEquals`
- Draft3, Draft4, Draft6, Draft7, Draft201909, Draft202012

**UniqueItemsValidator (all drafts):** `JsonElement.DeepEquals` → `JsonElementHelper.DeepEquals`
- Draft3, Draft4, Draft6, Draft7, Draft201909, Draft202012

**FrozenSet/FrozenDictionary validators:** Use `OptimizedSet`/`OptimizedDictionary`
- ~20 validators across all drafts

### B.5: Regenerate Compiled Validators
```bash
jsv-codegen generate-metaschemas -o ./JsonSchemaValidation/CompiledValidators/Generated -l ./JsonSchemaValidation
```

### B.6: Testing
```bash
dotnet build -c Release
dotnet test --framework net6.0
dotnet test --framework net8.0
dotnet test --framework net10.0
```

Benchmarks per target:
```bash
cd JsonSchemaValidation.Benchmarks
dotnet run -c Release -f net6.0 -- --filter *ValidationBenchmark*
dotnet run -c Release -f net8.0 -- --filter *ValidationBenchmark*
dotnet run -c Release -f net10.0 -- --filter *ValidationBenchmark*
```
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
## Compatibility Issues Identified

### 1. `JsonElement.DeepEquals()` - .NET 8.0+ only
- **Impact:** 28 validator files + 19 generated compiled validators
- **Used for:** `enum`, `const`, `uniqueItems` validation
- **Solution:** Create `JsonElementHelper.DeepEquals()` wrapper

### 2. `FrozenSet<T>` / `FrozenDictionary<K,V>` - .NET 8.0+ only
- **Impact:** ~20 validator files
- **Used for:** Property name lookups (performance optimization)
- **Solution:** Create `OptimizedSet<T>` and `OptimizedDictionary<K,V>` wrappers

### 3. `Lock` type - .NET 9.0+ only
- **Impact:** 2 files (`LruCache.cs`, `SchemaValidator.cs`)
- **Used for:** Thread synchronization
- **Solution:** Create `SyncLock` wrapper type

### 4. Code Generator emits `JsonElement.DeepEquals` directly
- **Impact:** All regenerated compiled validators
- **Solution:** Update generator to emit helper method call

## File Organization

```
JsonSchemaValidation/
├── Polyfills/
│   ├── JsonElementHelper.cs      # DeepEquals wrapper (net8+ native, net6 custom impl)
│   ├── OptimizedSet.cs           # FrozenSet wrapper (net8+ FrozenSet, net6 HashSet)
│   ├── OptimizedDictionary.cs    # FrozenDictionary wrapper (net8+ FrozenDictionary, net6 Dictionary)
│   └── SyncLock.cs               # Lock wrapper (net9+ Lock, net6/8 Monitor)
```

All `#if` directives are isolated to the `Polyfills/` folder. The rest of the codebase uses these wrapper types without any conditional compilation.

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| DeepEquals polyfill performance on net6 | Implement efficient recursive comparison; users wanting max perf should use net8+ |
| FrozenSet replacement perf impact on net6 | HashSet is still fast; document that net8+ is recommended for best performance |
| Conditional compilation complexity | All `#if` directives isolated to `Polyfills/` folder via wrapper types |
| Test matrix expansion (3x) | CI pipeline runs all three targets |
| Future API differences | Follow wrapper type pattern for any new version-specific APIs |

## Out of Scope

- netstandard2.0 support (too many API limitations)
- .NET Framework support (legacy, minimal demand)
- Separate NuGet packages per runtime (unnecessary complexity)

## Implementation Constraints

**No analyzer workarounds allowed:**
- Do NOT use `#pragma warning disable` to suppress analyzer errors
- Do NOT use `[SuppressMessage]` attributes to bypass static analysis
- Do NOT use any other workarounds to get past errors from static analysis packages
- All code must pass the existing analyzer rules (Microsoft.CodeAnalysis.NetAnalyzers, Roslynator, SonarAnalyzer, Meziantou.Analyzer, ClrHeapAllocationAnalyzer) cleanly

## Code Generator Target Framework Support

**Default behavior:** Generate code compatible with the runtime the generator is running on.
- net10 generator → generates net10-compatible code (uses `JsonElement.DeepEquals` directly)
- net8 generator → generates net8-compatible code (uses `JsonElement.DeepEquals` directly)
- net6 generator → generates net6-compatible code (uses `JsonElementHelper.DeepEquals`)

**Override option:** Allow specifying target framework explicitly.
- Running on net10, can generate net6-compatible code via CLI flag
- Example: `jsv-codegen generate -s schema.json -o ./Generated/ --target-framework net6.0`

**Implementation:**
- Add `--target-framework` / `-tfm` option to jsv-codegen CLI
- Code generator checks target framework to decide:
  - `JsonElement.DeepEquals()` vs `JsonElementHelper.DeepEquals()`
  - Any other framework-specific code patterns
- Default: auto-detect from `RuntimeInformation` or build-time constant

## Decision: Lock type

**Decision:** Use `lock(object)` everywhere instead of the .NET 9+ `Lock` type.

**Rationale:**
- The `Lock` type offers marginal performance gains in high-contention scenarios
- Schema caching is not a high-contention hot path
- Using `lock(object)` everywhere simplifies the codebase - no `SyncLock` wrapper needed
- Works identically on net6, net8, and net10

**Change:**
- Replace `Lock _lock = new()` with `object _lock = new()`
- Replace `_lock.EnterScope()` with standard `lock(_lock)` pattern
- Remove `SyncLock` from Polyfills plan
<!-- SECTION:NOTES:END -->
