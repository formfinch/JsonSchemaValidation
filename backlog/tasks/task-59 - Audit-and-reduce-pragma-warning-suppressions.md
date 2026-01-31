---
id: TASK-59
title: Audit and reduce pragma warning suppressions
status: To Do
assignee: []
created_date: '2026-01-31 12:48'
updated_date: '2026-01-31 12:51'
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
- [ ] #1 All suppression mechanisms audited (#pragma, GlobalSuppressions.cs, NoWarn, .editorconfig)
- [ ] #2 Unnecessary suppressions removed via refactoring
- [ ] #3 All suppressions have documented justification (no <Pending>)
- [ ] #4 Project-wide patterns consolidated in .editorconfig
- [ ] #5 No new suppressions added without documented justification
<!-- AC:END -->
