---
id: TASK-70
title: Audit git history for secrets and sensitive data
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
Before making the repository public, scan the entire git history for accidentally committed secrets or sensitive data. Once the repo is public, the full history is exposed — even data that was "deleted" in a later commit is recoverable.

**What to scan for:**
- API keys and tokens (NuGet, Azure, GitHub, etc.)
- Connection strings (database, service bus, etc.)
- Passwords and credentials
- Private keys and certificates
- OAuth client secrets
- Any `.env` or configuration files with real values

**Recommended tools (run at least two for coverage):**
- `gitleaks` — scans git history for secrets using regex patterns, widely used in CI
- `truffleHog` — entropy-based + regex detection across git history
- `git log -p | grep -i` for manual spot-checks of known sensitive patterns

**If secrets are found:**
- Rotate the secret immediately (the credential is compromised regardless of cleanup)
- Consider using `git filter-repo` or BFG Repo-Cleaner to rewrite history and remove the secret
- Document what was found and what was done about it

**Important:** This must be completed before the repo goes public. There is no undo — once history is exposed, it's exposed.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Full git history scanned with at least two detection tools (e.g., gitleaks + truffleHog)
- [ ] #2 No secrets or credentials found in any commit, or all found secrets rotated and history cleaned
- [ ] #3 Scan results documented (even if clean — proves the audit was done)
<!-- AC:END -->
