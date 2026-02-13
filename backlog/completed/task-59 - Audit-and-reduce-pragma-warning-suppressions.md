---
id: TASK-59
title: Audit and reduce pragma warning suppressions
status: Done
assignee: []
created_date: '2026-01-31 12:48'
updated_date: '2026-01-31 13:01'
labels:
  - code-quality
  - technical-debt
milestone: 'Phase 2: Code Quality & Testing'
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
**Context:**
The codebase has multiple mechanisms suppressing static analysis rules. While some may be justified, each suppression bypasses code quality enforcement and should be reviewed.

**Suppression mechanisms:**

### ✅ Accepted: `.editorconfig` severity overrides
These are the standard way to configure analyzer rules project-wide:
- `CA2025` = none (analyzer bug workaround)
- `IDE0008` = none (style preference for `var`)

### Needs review: `#pragma warning disable` (~50 instances)
- **Generated compiled validators** (~20): `HAA0603` in CompiledValidators/Generated/* → covered by TASK-58
- **Loop vs LINQ** (~30): `S3267`, `S1066` across keyword validators
- **Closure allocations** (4): `HAA0301`, `HAA0302` in SchemaValidatorFactory.cs
- **Method length** (1): `MA0051` in SchemaRepository.cs
- **Unused parameter** (1): `S1172` in SchemaValidatorFactory.cs

### Needs review: `GlobalSuppressions.cs` (1 instance)
- `IDE0008` (Use explicit type) - Justification: `<Pending>` ⚠️

### Needs review: `<NoWarn>` in .csproj (2 rules)
- `RS0026`, `RS0027` - PublicAPI analyzer back-compat warnings (may be justified for 1.0 release)

**Goal:**
Review inline suppressions and either:
- Remove by refactoring the code to satisfy the analyzer
- Move to .editorconfig if it's a project-wide preference
- Document justification if inline suppression is truly necessary
- Remove any `<Pending>` justifications

**Exclusions:**
- .editorconfig suppressions (accepted as standard configuration)
- Generated compiled validator code (covered by TASK-58)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 All suppression mechanisms audited (#pragma, GlobalSuppressions.cs, NoWarn, .editorconfig)
- [x] #2 Unnecessary suppressions removed via refactoring
- [x] #3 All suppressions have documented justification (no <Pending>)
- [x] #4 Project-wide patterns consolidated in .editorconfig
- [x] #5 No new suppressions added without documented justification
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
## Summary

**Removed/Consolidated:**
- Deleted `GlobalSuppressions.cs` (redundant - IDE0008 already in .editorconfig)
- Moved S3267 (LINQ loops) and S1066 (mergeable if) to .editorconfig
- Removed ~26 inline `#pragma` directives from keyword validators across all 6 drafts

**Remaining justified suppressions:**
- `HAA0301/0302` in SchemaValidatorFactory.cs - Closure for lazy validator creation (not in hot path)
- `MA0051` in SchemaRepository.cs - Complex schema walking method (inherent complexity)
- `S1172` in SchemaValidatorFactory.cs - Unused parameter (noted: dead code around this should be cleaned up separately)
- `HAA0603` in CompiledValidators/Generated/* - Covered by TASK-58

**Net reduction:** -70 lines, 29 files changed, ~26 inline pragmas eliminated
<!-- SECTION:FINAL_SUMMARY:END -->
