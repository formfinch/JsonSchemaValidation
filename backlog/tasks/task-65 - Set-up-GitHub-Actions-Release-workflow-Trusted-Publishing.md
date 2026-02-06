---
id: TASK-65
title: Set up GitHub Actions - Release workflow + Trusted Publishing
status: To Do
assignee: []
created_date: '2026-02-05 22:13'
updated_date: '2026-02-06 13:42'
labels:
  - nuget
  - ci-cd
  - github-actions
milestone: 1.0.0 Release
dependencies:
  - TASK-33
documentation:
  - 'https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing'
  - >-
    https://andrewlock.net/easily-publishing-nuget-packages-from-github-actions-with-trusted-publishing/
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Create a GitHub Actions workflow to publish the FormFinch.JsonSchemaValidation package to nuget.org when a version tag is pushed. Uses NuGet Trusted Publishing (OIDC) — no stored API keys.

**Workflow triggers:**
- Push of version tag (e.g., `v1.0.0`)
- Manual workflow dispatch (for re-publishing or recovery)

**Jobs:**
- Checkout + setup .NET SDK (8.0.x and 10.0.x for multi-targeting)
- Build release configuration
- Run unit tests (sanity check before publish)
- `dotnet pack -c Release` with version derived from the git tag (strip `v` prefix)
- Use `NuGet/login@v1` to exchange OIDC token for temporary NuGet API key
- Push `.nupkg` and `.snupkg` to `https://api.nuget.org/v3/index.json`
- Create GitHub Release with changelog notes

**Permissions:**
- `contents: read` (checkout) + `write` (create GitHub Release)
- `id-token: write` (OIDC for NuGet Trusted Publishing)

**NuGet.org Trusted Publishing setup:**
Go to https://www.nuget.org/account/trustedpublishing and create a policy:
- GitHub Repository Owner: `formfinch`
- Repository Name: the repo name
- Workflow File: must match the filename chosen (e.g., `release.yml`)
- Environment: leave blank

**Important:**
- The workflow filename must exactly match the nuget.org trusted publishing policy
- No API key secrets stored in GitHub — OIDC handles authentication
- No benchmarks/memory/stress tests here — nightly already validated quality
- Absorbs scope from archived TASK-34 (GitHub Release creation)

**Docs:**
- https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing
- https://andrewlock.net/easily-publishing-nuget-packages-from-github-actions-with-trusted-publishing/
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Trusted Publishing policy configured on nuget.org with correct repo owner, repo name, and workflow filename
- [ ] #2 `.github/workflows/release.yml` created, triggers on version tag push (`v*`) and manual dispatch
- [ ] #3 Workflow uses `NuGet/login@v1` OIDC flow (no stored API keys)
- [ ] #4 Workflow builds and packs for both net8.0 and net10.0
- [ ] #5 Version derived from git tag (e.g., `v1.0.0` → `1.0.0`)
- [ ] #6 `.snupkg` symbol packages published alongside `.nupkg`
- [ ] #7 GitHub Release created with changelog notes
- [ ] #8 Package successfully published to nuget.org via the workflow
<!-- AC:END -->
