---
id: TASK-18
title: Add PublicAPI analyzers
status: Done
assignee: []
created_date: '2026-01-30 21:56'
updated_date: '2026-01-31 10:51'
labels:
  - api-stability
  - tooling
milestone: 'Phase 3: API Stability'
dependencies:
  - TASK-17
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Add `Microsoft.CodeAnalysis.PublicApiAnalyzers` to track public API changes and prevent accidental breaking changes.

This creates `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` files that track the public API surface.

**Important:** Only add this after API usability refinement is complete. This locks down the API.

**Implementation completed:**
- Added `Microsoft.CodeAnalysis.PublicApiAnalyzers` v3.3.4 package
- Created `PublicAPI.Shipped.txt` (empty, with `#nullable enable`)
- Created `PublicAPI.Unshipped.txt` with full public API surface (~200 entries)
- Suppressed RS0026/RS0027 backcompat warnings (not applicable to 1.0.0 initial release)
- All 2341 tests pass
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 PublicApiAnalyzers package added
- [ ] #2 PublicAPI.Shipped.txt baseline created
- [ ] #3 Build fails on undocumented public API changes
<!-- AC:END -->
