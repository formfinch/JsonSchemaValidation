---
id: TASK-25
title: Consider strong naming
status: To Do
assignee: []
created_date: '2026-01-30 21:56'
updated_date: '2026-01-31 10:50'
labels:
  - nuget-package
  - enterprise
  - decision
milestone: 'Phase 4: Package Configuration'
dependencies: []
priority: low
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Some enterprise customers require strongly-named assemblies. Evaluate whether to support this.

**Trade-offs:**
- Pro: Required by some enterprises, can be loaded in strongly-named apps
- Con: Adds complexity, key management, version binding issues
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Decision made on strong naming
- [ ] #2 If yes, signing key created and configured
<!-- AC:END -->
