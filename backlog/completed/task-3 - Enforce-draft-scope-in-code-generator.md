---
id: TASK-3
title: Enforce draft scope in code generator
status: Done
assignee: []
created_date: '2026-01-30 21:54'
updated_date: '2026-01-31 10:51'
labels:
  - code-generator
  - correctness
  - architecture
milestone: 'Phase 1: Architecture & Core Correctness'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The code generator (`jsv-codegen`) now has full draft-aware support:
- Detects `$schema` and generates draft-specific code
- Draft-specific format validators (FormatValidators.cs per draft namespace)
- Draft-specific keyword semantics (items/prefixItems, dependencies, etc.)
- Supports all drafts: Draft 3, 4, 6, 7, 2019-09, 2020-12

**Implementation:**
- `SchemaDraft` enum and `SchemaDraftDetector` for $schema detection
- All keyword code generators check `DetectedDraft` for draft-appropriate behavior
- `RecursiveRefCodeGenerator` for Draft 2019-09 `$recursiveRef` support
- `DynamicRefCodeGenerator` for Draft 2020-12 `$dynamicRef` support
- Backwards-compat: `dependencies` supported in 2019-09+ when alone (test policy)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Code generator validates $schema and enforces supported drafts
- [ ] #2 Clear error message when unsupported draft is detected
- [ ] #3 Draft-specific keyword code generation for all drafts
- [ ] #4 Format validator imports are draft-aware
<!-- AC:END -->
