---
id: TASK-37
title: Set up branch protection
status: To Do
assignee: []
created_date: '2026-01-30 21:57'
updated_date: '2026-01-31 10:50'
labels:
  - infrastructure
  - github
milestone: 'Phase 6: Release Infrastructure'
dependencies:
  - TASK-33
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Configure GitHub branch protection rules.

**Rules for main branch:**
- Require pull request before merge
- Require status checks to pass
- Require up-to-date branches
- Optional: Require review approval
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Branch protection enabled
- [ ] #2 Direct push to main blocked
<!-- AC:END -->
