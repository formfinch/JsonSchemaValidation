---
id: TASK-30
title: Create KNOWN_LIMITATIONS.md
status: To Do
assignee: []
created_date: '2026-01-30 21:57'
updated_date: '2026-01-31 10:50'
labels:
  - documentation
  - user-facing
milestone: 'Phase 5: Documentation & Samples'
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Create a document listing known limitations and edge cases that users should be aware of.

**Initial limitations to document:**
- Schema hashing for numbers beyond double precision (~15-17 significant digits) may collide. Two schemas differing only in very large numbers could hash identically. Practical impact is minimal since schemas rarely contain such numbers.
- Static API schema cache excludes `$id` from hash for performance. Schemas differing only by `$id` will share a cached validator. This means: (1) internal `$ref: "#"` resolves to the first schema's base URI, (2) output locations show the first schema's URI, (3) the second schema's `$id` is never registered. The boolean valid/invalid result is unaffected in most cases. Use the DI-based API if `$id` correctness matters.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 KNOWN_LIMITATIONS.md created
- [ ] #2 Linked from README
- [ ] #3 Each limitation explains impact and workarounds if any
<!-- AC:END -->
