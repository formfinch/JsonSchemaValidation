---
id: TASK-5
title: Add compiled validator tests for all drafts
status: Done
assignee: []
created_date: '2026-01-30 21:54'
updated_date: '2026-01-31 10:51'
labels:
  - testing
  - compiled-validators
  - high-priority
milestone: 'Phase 2: Code Quality & Testing'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The compiled validators have pre-generated metaschemas for all 6 drafts (3, 4, 6, 7, 2019-09, 2020-12) but tests only exist for Draft 2020-12. This creates a blind spot where compiled validators for older drafts may have bugs that go undetected.

**Implementation completed:**
Created compiled validation test classes for each draft that run the JSON-Schema-Test-Suite tests against the compiled validators.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Compiled tests exist for all 6 drafts
- [ ] #2 Tests run against JSON-Schema-Test-Suite
- [ ] #3 Test results are visible (not silently skipped)
- [ ] #4 Coverage gap documented for each draft
<!-- AC:END -->
