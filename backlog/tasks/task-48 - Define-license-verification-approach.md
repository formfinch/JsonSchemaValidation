---
id: TASK-48
title: Define license verification approach
status: To Do
assignee: []
created_date: '2026-01-30 21:58'
updated_date: '2026-01-31 10:50'
labels:
  - commercial
  - policy
  - decision
milestone: 'Phase 8: Commercial'
dependencies:
  - TASK-46
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Decide how commercial users prove they're licensed.

**Options:**
- **Honor system** - Common for dev tools, no technical enforcement
- **License key in code** - User adds key to configuration (LemonSqueezy can generate)
- **Organization registry** - Maintain list of licensed organizations
- **NuGet package variant** - Separate "Pro" package for commercial users

**Recommendation:** Start with honor system (like most dev tool libraries), consider adding license key validation later if needed.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Verification approach decided and documented
- [ ] #2 If technical enforcement chosen, implementation planned
<!-- AC:END -->
