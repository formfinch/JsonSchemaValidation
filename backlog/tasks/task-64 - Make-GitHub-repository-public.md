---
id: TASK-64
title: Make GitHub repository public
status: To Do
assignee: []
created_date: '2026-02-04 23:16'
updated_date: '2026-02-06 13:48'
labels:
  - infrastructure
  - github
milestone: 'Phase 7: Community'
dependencies:
  - TASK-35
  - TASK-70
  - TASK-71
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Switch the GitHub repository from private to public after the package is published and all audits are clean.

**Prerequisites (must all be complete):**
- TASK-35: First package version published to nuget.org (proves the package works end-to-end)
- TASK-70: Git history audited for secrets — no exposed credentials
- TASK-71: Codebase audited for internal references — nothing private leaks

**What to do:**
1. Confirm all prerequisite audits are clean
2. Go to GitHub repo → Settings → Danger Zone → Change repository visibility → Public
3. Verify the repo is accessible without authentication

**Benefits of going public:**
- Unlimited free GitHub Actions minutes (eliminates CI cost concern)
- Free CodeQL static analysis (consider enabling after going public)
- Free GitHub Advanced Security features
- Community can inspect source, file issues, contribute
- Builds trust for NuGet package consumers (Source Link already points here)

**Post-public follow-up:**
- Consider enabling CodeQL code scanning (free on public repos) — would replace the gap left by not having GHAS on private
- Review GitHub Actions workflow costs — can relax trigger restrictions (e.g., add push-to-main CI) since minutes are now free
- Verify Dependabot/secret scanning still active (they should carry over)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 All prerequisite audits completed and clean (TASK-70, TASK-71)
- [ ] #2 Repository visibility changed to public on GitHub
- [ ] #3 Repository accessible without authentication
- [ ] #4 Existing CI/CD workflows still function correctly after visibility change
- [ ] #5 Consider enabling CodeQL scanning (free on public repos)
<!-- AC:END -->
