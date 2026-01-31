---
id: TASK-8
title: Document compiled validator limitations
status: Done
assignee: []
created_date: '2026-01-30 21:55'
updated_date: '2026-01-31 10:51'
labels:
  - documentation
  - compiled-validators
milestone: 'Phase 2: Code Quality & Testing'
dependencies:
  - TASK-6
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Create clear documentation of what compiled validators cannot do compared to dynamic validators.

**Known limitations (documented in-line):**
1. ~~**$dynamicRef with runtime scope resolution**~~ - ✅ RESOLVED via TASK-048
2. ~~**$recursiveRef with runtime scope resolution**~~ - ✅ RESOLVED via TASK-048
3. **Remote refs with internal $ref** - Cannot compile subschemas referencing siblings
4. **Vocabulary-based validation** - Cannot enable/disable keywords via $vocabulary
5. **Cross-draft compatibility** - Cannot process $ref targets according to declared $schema

**Note:** Limitations 1-2 have been resolved with the scope stack implementation (TASK-048). Limitations 3-5 are fundamental to the compiled approach and are documented as skipped test reasons.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 All limitations documented with explanations
- [ ] #2 Workarounds documented where applicable
- [ ] #3 Users can make informed decision between dynamic/compiled
<!-- AC:END -->
