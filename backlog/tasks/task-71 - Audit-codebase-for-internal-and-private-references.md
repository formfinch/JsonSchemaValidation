---
id: TASK-71
title: Audit codebase for internal and private references
status: To Do
assignee: []
created_date: '2026-02-06 13:47'
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
- [ ] #1 All source files, configs, and documentation reviewed for internal references
- [ ] #2 Git commit messages audited for internal references
- [ ] #3 Decision made on whether backlog/ folder is included in the public repo
- [ ] #4 All internal references removed or replaced with public-facing equivalents
- [ ] #5 Audit findings documented
<!-- AC:END -->
