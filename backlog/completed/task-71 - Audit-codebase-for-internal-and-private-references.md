---
id: TASK-71
title: Audit codebase for internal and private references
status: Done
assignee: []
created_date: '2026-02-06 13:47'
updated_date: '2026-02-13 15:50'
labels:
  - security
  - infrastructure
milestone: 'Phase 7: Community'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Before making the repository public, audit the codebase and git history for references to internal infrastructure, private systems, or information that shouldn't be publicly visible.

**What to check for:**
- Internal URLs (Azure DevOps, private wikis, intranet sites, internal APIs)
- Private domain names or IP addresses
- Employee names or personal email addresses in code/comments (as opposed to public-facing author info in the csproj/LICENSE which is intentional)
- Internal project names, codenames, or identifiers
- References to internal NuGet feeds or package sources
- Comments containing internal discussion, TODOs referencing internal systems, or proprietary business logic notes
- Any files that were meant to be temporary or internal-only

**Where to check:**
- All source files and comments
- Configuration files (nuget.config, .editorconfig, launchSettings.json, etc.)
- Git commit messages (the full history)
- CI/CD configuration files
- Documentation and markdown files
- Backlog task descriptions (if the backlog folder will be included in the public repo)

**Note:** TASK-36 (Update nuget.config for public release) handles the Azure DevOps feed reference specifically. This task is a broader sweep for anything else that might have been missed.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 All source files, configs, and documentation reviewed for internal references
- [x] #2 Git commit messages audited for internal references
- [x] #3 Decision made on whether backlog/ folder is included in the public repo
- [x] #4 All internal references removed or replaced with public-facing equivalents
- [x] #5 Audit findings documented
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
**Internal references audit complete.**\n\n**Findings and actions:**\n\n| Finding | Severity | Action |\n|---|---|---|\n| Private Azure DevOps NuGet feed in `nuget.config` | HIGH | Replaced with nuget.org (PR #19) |\n| Redundant feed override step in `nightly.yml` | HIGH | Removed (PR #19) |\n| Azure DevOps source URL in backlog task-32 | HIGH | Scrubbed (PR #19) |\n| Personal email in early git history | LOW | Accepted — no security risk, recent commits use noreply |\n| CI comment about private feed | LOW | Removed along with the redundant step |\n\n**Decision: backlog/ folder included in public repo** — tasks are public, no sensitive content remaining.\n\n**No action needed for:**\n- Author name in .csproj (intentional public metadata)\n- support@formfinch.com in COMMERCIAL.md (intentional contact info)\n- test@formfinch.com in test data (standard practice)\n\n**Note:** TASK-36 (nuget.config update) is now effectively resolved by this work."
<!-- SECTION:FINAL_SUMMARY:END -->
