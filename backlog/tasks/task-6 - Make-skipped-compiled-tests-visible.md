---
id: TASK-6
title: Make skipped compiled tests visible
status: Done
assignee: []
created_date: '2026-01-30 21:54'
updated_date: '2026-01-31 10:51'
labels:
  - testing
  - transparency
  - high-priority
milestone: 'Phase 2: Code Quality & Testing'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The current `IsTestDisabled()` method in `CompiledSchemaValidationTests.cs` silently skips 21+ tests without reporting them. This hides known gaps from users and developers.

**Implementation completed:**
Skipped tests are now visible in test output with documented skip reasons.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Skipped tests visible in test output (not silently ignored)
- [ ] #2 Skip reasons documented for each test category
- [ ] #3 Total skipped count easily discoverable
<!-- AC:END -->
