---
id: TASK-51
title: Reduce allocations in property validation
status: To Do
assignee: []
created_date: '2026-01-30 21:58'
updated_date: '2026-01-31 10:50'
labels:
  - performance
  - optimization
milestone: 'Phase 9: Dynamic Validator Performance'
dependencies:
  - TASK-50
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Based on profiling results, reduce allocations in property-related validators which likely dominate complex schema validation.

**Potential optimizations:**
- Pool or reuse property name enumerators
- Use `Span<T>` or `ArraySegment<T>` where possible
- Avoid LINQ allocations in hot paths
- Consider struct-based iterators
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Property validation allocations reduced by 50%+
- [ ] #2 No regression in validation correctness (all tests pass)
- [ ] #3 Benchmarks show measurable improvement
<!-- AC:END -->
