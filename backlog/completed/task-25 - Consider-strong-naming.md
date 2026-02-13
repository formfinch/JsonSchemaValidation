---
id: TASK-25
title: Consider strong naming
status: Done
assignee: []
created_date: '2026-01-30 21:56'
updated_date: '2026-02-13 22:00'
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
- [x] #1 Decision made on strong naming
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
**Decision: No strong naming.**\n\nStrong naming will not be applied at this time. The package needs to prove its value first before investing in enterprise-specific features. Can be revisited later if users request it — adding signing is a non-breaking change."
<!-- SECTION:FINAL_SUMMARY:END -->
