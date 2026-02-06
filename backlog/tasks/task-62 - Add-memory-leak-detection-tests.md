---
id: TASK-62
title: Add memory leak detection tests
status: To Do
assignee: []
created_date: '2026-02-02 21:47'
updated_date: '2026-02-06 13:43'
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
- [ ] #1 Memory leak tests exist for SchemaRepository
- [ ] #2 Memory leak tests exist for LruCache
- [ ] #3 Memory leak tests exist for Static API repeated usage
- [ ] #4 Memory leak tests exist for compiled validator lifecycle
- [ ] #5 Tests run as part of the nightly workflow (TASK-67), not PR CI
- [ ] #6 No memory leaks detected in current implementation
<!-- AC:END -->
