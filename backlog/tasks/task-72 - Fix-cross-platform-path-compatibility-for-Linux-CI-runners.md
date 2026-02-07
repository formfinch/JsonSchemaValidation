---
id: TASK-72
title: Fix cross-platform path compatibility for Linux CI runners
status: To Do
assignee: []
created_date: '2026-02-06 23:43'
labels:
  - infrastructure
  - ci-cd
  - testing
milestone: 1.0.0 Release
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Test files throughout the codebase use hardcoded Windows backslash paths (e.g., `@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2019-09"`) which don't resolve on Linux. This forces CI to use `windows-latest` runners, which are slower (~7m vs ~2-3m target) and more expensive.

**Affected files:** All test files that reference the JSON-Schema-Test-Suite submodule across Draft3, Draft4, Draft6, Draft7, Draft201909, Draft202012, and their compiled variants. Paths are used both in `LoadTestCases()` calls and direct `remotesPath` variables.

**Fix approach:** Replace all hardcoded backslash path strings with `Path.Combine()` or forward slashes (which .NET handles cross-platform). Once fixed, switch CI workflow (`.github/workflows/ci.yml`) from `windows-latest` back to `ubuntu-latest` for faster, cheaper runs.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 All test file paths use cross-platform compatible path construction
- [ ] #2 CI workflow switched to ubuntu-latest
- [ ] #3 All tests pass on Linux runner
- [ ] #4 CI run time under 3 minutes
<!-- AC:END -->
