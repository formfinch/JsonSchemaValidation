---
id: TASK-1
title: Thread-safety audit
status: Done
assignee: []
created_date: '2026-01-30 21:54'
updated_date: '2026-01-31 10:51'
labels:
  - reliability
  - concurrency
  - code-quality
milestone: 'Phase 1: Architecture & Core Correctness'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Comprehensive review of all concurrent code paths to ensure thread-safety guarantees are correctly implemented and documented.

**Recent fix context:**
`CompiledValidatorRegistry` was found to use plain `Dictionary`/`HashSet` despite claiming thread-safety. This was fixed by switching to `ConcurrentDictionary`. A systematic audit is needed to find similar issues.

**Areas to audit:**
- All classes using concurrent collections - verify correct usage patterns
- Singleton services - ensure no mutable shared state without synchronization
- Schema repository and related caches
- Validator factories and lazy initialization
- Context factories and validation contexts
- Static API caches (`JsonSchemaValidator.SchemaCache`)

**Implementation completed (commit 47514b4):**
- `SchemaValidator.cs` - Added `Lock` + `volatile` for cache initialization
- `RefValidator.cs` (all 6 drafts) - Replaced manual cache with thread-safe `Lazy<T>` using `LazyThreadSafetyMode.ExecutionAndPublication`
- `JsonPointer.cs` - Made lazy string caching thread-safe with `Interlocked.CompareExchange`
- `SchemaRepository.cs` - Uses `ConcurrentDictionary` + volatile snapshot pattern for `_sortedSchemas`
- `CompiledValidatorRegistry.cs` - Uses `ConcurrentDictionary` for all three collections
- `JsonSchemaValidator.cs` - Uses `Lazy<T>` with `ExecutionAndPublication` + `ConcurrentDictionary` for schema cache
- Thread-safety XML documentation added to all public types
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 All concurrent code paths reviewed
- [ ] #2 No plain collections used where concurrent access is possible
- [ ] #3 Thread-safety documented for public types
- [ ] #4 Any fixes verified with concurrent tests
<!-- AC:END -->
