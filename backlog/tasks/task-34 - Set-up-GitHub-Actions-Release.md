---
id: TASK-34
title: Set up GitHub Actions - Release
status: To Do
assignee: []
created_date: '2026-01-30 21:57'
updated_date: '2026-01-31 10:50'
labels:
  - infrastructure
  - ci-cd
milestone: 'Phase 6: Release Infrastructure'
dependencies:
  - TASK-33
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Create GitHub Actions workflow for releasing to NuGet.org.

**Workflow triggers:**
- Push of version tag (e.g., `v1.0.0`)
- Manual workflow dispatch

**Jobs:**
- Build release configuration
- Run tests
- Create NuGet package
- Publish to NuGet.org
- Create GitHub release with notes
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 .github/workflows/release.yml created
- [ ] #2 NuGet API key stored as secret
- [ ] #3 Tag push triggers release
- [ ] #4 GitHub release created with changelog
<!-- AC:END -->
