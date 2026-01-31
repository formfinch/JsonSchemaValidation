---
id: TASK-19
title: Define versioning strategy
status: Done
assignee: []
created_date: '2026-01-30 21:56'
updated_date: '2026-01-31 11:18'
labels:
  - api-stability
  - documentation
milestone: 'Phase 3: API Stability'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Document semantic versioning policy. Define what constitutes major/minor/patch version bumps.

**Suggested policy:**
- **Major:** Breaking API changes, removed functionality
- **Minor:** New features, new keywords supported, non-breaking additions
- **Patch:** Bug fixes, performance improvements, documentation
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Versioning policy documented
- [x] #2 Policy added to CONTRIBUTING.md or separate VERSIONING.md
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added versioning section to CONTRIBUTING.md following Semantic Versioning 2.0.0 standard. Documents what constitutes MAJOR/MINOR/PATCH version changes specific to a JSON Schema validation library (e.g., dropping draft support = major, new keywords = minor).
<!-- SECTION:FINAL_SUMMARY:END -->
