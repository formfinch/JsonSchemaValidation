---
id: TASK-32
title: Create GitHub repository
status: In Progress
assignee: []
created_date: '2026-01-30 21:57'
updated_date: '2026-01-31 22:45'
labels:
  - infrastructure
  - github
milestone: 'Phase 6: Release Infrastructure'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Migrate repository from Azure DevOps to GitHub as a private repository.

**Source:** `https://formfinch.visualstudio.com/formfinch-next/_git/JsonSchemaValidation`
**Target:** `https://github.com/formfinch/JsonSchemaValidation` (private)

**Current state:**
- Branches: master, feature/vocabulary, origin/main
- Tags: None

Migration will preserve full git history.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Repository created and public
- [x] #2 Code pushed
- [x] #3 Repository URL updated in .csproj
<!-- AC:END -->
