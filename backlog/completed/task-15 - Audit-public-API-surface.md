---
id: TASK-15
title: Audit public API surface
status: Done
assignee: []
created_date: '2026-01-30 21:55'
updated_date: '2026-01-31 10:51'
labels:
  - api-stability
  - breaking-change-risk
milestone: 'Phase 3: API Stability'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Review all `public` types and members. Ensure only intentionally public APIs are exposed. Everything else should be `internal`.

**Key areas to audit:**
- `DependencyInjection/` - Entry points for users
- `Validation/` - Core validation interfaces
- `Common/` - Shared types
- All validator factories and validators

**Implementation completed:**
- ~30 implementation details changed from `public` to `internal`
- Added `InternalsVisibleTo` for test, compiler, codegen, and benchmark projects
- Discovered 3 types must remain public for runtime code generation
- Full details in `docs/PUBLIC_API_AUDIT.md`
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 All public types intentionally public
- [ ] #2 Implementation details marked internal
- [ ] #3 Public API design decisions documented (see docs/PUBLIC_API_AUDIT.md)
<!-- AC:END -->
