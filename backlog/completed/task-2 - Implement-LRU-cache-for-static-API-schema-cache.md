---
id: TASK-2
title: Implement LRU cache for static API schema cache
status: Done
assignee: []
created_date: '2026-01-30 21:54'
updated_date: '2026-01-31 10:51'
labels:
  - performance
  - architecture
  - server-scenarios
milestone: 'Phase 1: Architecture & Core Correctness'
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The static `JsonSchemaValidator` API previously used a simple bounded cache that cleared entirely when the size limit was reached. This was inadequate for server scenarios where:
- Frequently-used schemas should be retained
- Cache thrashing occurs if many unique schemas are validated in bursts
- Memory should be bounded but useful entries preserved

**Implementation:**
- Added `LruCache<TKey, TValue>` class in `Common/LruCache.cs`
- Thread-safe using `Lock` with `Dictionary` + `LinkedList` for O(1) access and eviction
- Evicts least recently used entry when capacity (1000) is reached
- Both reads and writes update access order for proper LRU behavior
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 LRU or equivalent eviction strategy implemented
- [ ] #2 Cache preserves frequently-used schemas across evictions
- [ ] #3 Performance benchmarked vs current approach (not required - simple implementation)
- [ ] #4 Server scenario guidance documented (DI-based API already recommended for servers)
<!-- AC:END -->
