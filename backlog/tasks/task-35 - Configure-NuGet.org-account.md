---
id: TASK-35
title: Configure NuGet.org account
status: To Do
assignee: []
created_date: '2026-01-30 21:57'
updated_date: '2026-01-31 10:50'
labels:
  - infrastructure
  - nuget
milestone: 'Phase 6: Release Infrastructure'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Set up NuGet.org for package publishing.

**Tasks:**
- Create/configure NuGet.org organization account
- Reserve package ID `FormFinch.JsonSchemaValidation`
- Generate API key for publishing
- Add API key to GitHub secrets
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Package ID reserved
- [ ] #2 API key configured in GitHub
- [ ] #3 Test publish works (can use unlisted package)
<!-- AC:END -->
