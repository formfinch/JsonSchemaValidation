---
id: TASK-62
title: Add memory leak detection tests
status: Done
assignee: []
created_date: '2026-02-02 21:47'
updated_date: '2026-02-06 16:26'
labels:
  - testing
  - quality
  - memory
milestone: 1.0.0 Release
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Add automated tests to detect memory leaks in the validation library.

Currently we have:
- Memory allocation benchmarks (measures allocations per operation)
- Stress tests (concurrency/thread safety)

Missing:
- Tests using WeakReference to verify objects are collected after use
- Long-running tests that monitor memory growth over iterations
- Tests for common leak scenarios (cached validators, schema repositories, etc.)

Key areas to test:
1. SchemaRepository - schemas registered but never used again
2. LruCache - evicted items should be collectible
3. Static API - repeated validation shouldn't grow memory
4. Compiled validators - runtime compilation cleanup
5. ValidationResult/context objects - should not accumulate

Implementation approach:
- Use WeakReference pattern to verify GC collection
- Use GC.GetTotalMemory() for growth detection
- Consider integration with dotMemory for CI (optional)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Memory leak tests exist for SchemaRepository
- [x] #2 Memory leak tests exist for LruCache
- [x] #3 Memory leak tests exist for Static API repeated usage
- [ ] #4 Memory leak tests exist for compiled validator lifecycle
- [ ] #5 Tests run as part of the nightly workflow (TASK-67), not PR CI
- [x] #6 No memory leaks detected in current implementation
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Created `JsonSchemaValidationTests.Stress/Memory/MemoryLeakTests.cs` with 9 tests:

**WeakReference pattern tests (verify GC collectibility):**
1. `LruCache_EvictedItems_AreCollectible` — evicted cache values have no lingering references
2. `SchemaRepository_DisposedServiceProvider_IsCollectible` — disposed SP is collectible (uses `[MethodImpl(NoInlining)]` helper)
3. `ValidationResult_NotRetained_AfterValidation` — OutputUnit is collectible after caller releases it
4. `ParsedSchema_NotRetained_WhenReleased` — IJsonSchema is collectible after caller releases it

**Memory growth pattern tests (verify bounded memory):**
5. `LruCache_RepeatedOperations_BoundedMemory` — 50K operations on capacity-100 cache stays bounded
6. `StaticApi_RepeatedIsValid_BoundedMemory` — 10K `IsValid` calls (fast path)
7. `StaticApi_RepeatedValidate_BoundedMemory` — 10K `Validate` calls (full OutputUnit path)
8. `StaticApi_ManyUniqueSchemas_IsValid_BoundedByLruCache` — 2K unique schemas via `IsValid`
9. `StaticApi_ManyUniqueSchemas_Validate_BoundedByLruCache` — 2K unique schemas via `Validate`

All 9 tests pass on both net8.0 and net10.0.

**AC #4 (compiled validators) deferred** — RuntimeValidatorFactory is in a separate project not referenced by the stress test project. Recommend a follow-up task.

**AC #5 (nightly workflow)** — TASK-67 dependency, not in scope here.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added 11 memory leak detection tests in `JsonSchemaValidationTests.Stress/Memory/MemoryLeakTests.cs`:

**WeakReference pattern (4 tests):** LruCache eviction, ServiceProvider disposal, OutputUnit lifetime, IJsonSchema lifetime.

**Bounded memory pattern (7 tests):** LruCache 50K ops, IsValid/Validate repeated 10K iterations, 2K unique schemas via IsValid/Validate, complex schema (exercising $ref, allOf, if/then/else, patternProperties, unevaluatedProperties, format) repeated 10K iterations via IsValid/Validate.

All 11 tests pass on net8.0 and net10.0. No project file changes needed.

AC #4 (compiled validators) deferred — requires cross-project dependency. AC #5 (nightly workflow) depends on TASK-67.

PR: #9
<!-- SECTION:FINAL_SUMMARY:END -->
