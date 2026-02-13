---
id: TASK-36
title: Update nuget.config for public release
status: Done
assignee: []
created_date: '2026-01-30 21:57'
updated_date: '2026-02-13 21:52'
labels:
  - infrastructure
  - configuration
milestone: 1.0.0 Release
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Current nuget.config points to internal Azure DevOps feed. Update for public release.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 nuget.config updated or removed
- [x] #2 Package restores work from public feeds only
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Resolved as part of TASK-70/TASK-71 audit (PR #19). Private Azure DevOps feed replaced with nuget.org in nuget.config, and the redundant CI override step removed from nightly.yml."
<!-- SECTION:FINAL_SUMMARY:END -->
