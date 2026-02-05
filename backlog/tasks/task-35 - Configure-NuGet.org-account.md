---
id: TASK-35
title: Configure NuGet.org account
status: To Do
assignee: []
created_date: '2026-01-30 21:57'
updated_date: '2026-02-05 22:23'
labels:
  - infrastructure
  - nuget
milestone: 'Phase 6: Release Infrastructure'
dependencies:
  - TASK-65
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Set up NuGet.org for package publishing.

**Tasks:**
1. Create/configure NuGet.org account
2. Configure Trusted Publishing (OIDC) policy on nuget.org — linked to the workflow from TASK-65
3. Publish first version of `FormFinch.JsonSchemaValidation` — this claims the package ID (there is no way to reserve a specific name ahead of time; publishing is what claims it)
4. Apply for `FormFinch.` prefix reservation (TASK-66) after the first package is published

**Decisions made:**
- **No author signing** — skipped for now. NuGet.org automatically adds its own repository signature. Author signing is optional, rarely used (~15% of top packages), and adds cost/complexity. Can be added later if enterprise customers request it.
- **Trusted Publishing over API keys** — uses GitHub Actions OIDC tokens to obtain temporary, single-use NuGet API keys. No long-lived secrets to store or rotate. See TASK-65 for workflow implementation details.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 NuGet.org account created and configured
- [ ] #2 Trusted Publishing policy configured on nuget.org (linked to GitHub repo and workflow file from TASK-65)
- [ ] #3 First package version published to nuget.org, claiming the FormFinch.JsonSchemaValidation package ID
<!-- AC:END -->
