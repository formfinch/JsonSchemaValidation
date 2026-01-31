---
id: TASK-54
title: Benchmark parity target - match LateApex performance
status: To Do
assignee: []
created_date: '2026-01-30 21:58'
updated_date: '2026-01-31 10:50'
labels:
  - performance
  - milestone
milestone: 'Phase 9: Dynamic Validator Performance'
dependencies:
  - TASK-51
  - TASK-52
  - TASK-53
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
After implementing optimizations, verify that FormFinch dynamic validator achieves competitive performance with LateApex in complex scenarios.

**Target metrics (Complex/Valid scenario):**
- Execution time: Within 1.5x of LateApex (currently 1.6x slower)
- Allocations: Within 2x of LateApex (currently 3x higher)

**Note:** LateApex lacks `unevaluatedProperties`/`unevaluatedItems` support, so some overhead is acceptable. The goal is competitive performance, not parity.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Complex schema validation within 1.5x of LateApex time
- [ ] #2 Allocations within 2x of LateApex
- [ ] #3 Simple/Medium scenarios still faster than competitors
- [ ] #4 Updated benchmark results documented
<!-- AC:END -->
