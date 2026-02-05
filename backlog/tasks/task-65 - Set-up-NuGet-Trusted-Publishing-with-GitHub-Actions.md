---
id: TASK-65
title: Set up NuGet Trusted Publishing with GitHub Actions
status: To Do
assignee: []
created_date: '2026-02-05 22:13'
labels:
  - nuget
  - ci-cd
  - github-actions
dependencies: []
documentation:
  - 'https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing'
  - >-
    https://andrewlock.net/easily-publishing-nuget-packages-from-github-actions-with-trusted-publishing/
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Configure NuGet.org Trusted Publishing (OIDC-based) and create a GitHub Actions workflow to publish the FormFinch.JsonSchemaValidation package to nuget.org when a version tag is pushed.

**Background:**
- NuGet.org supports Trusted Publishing via GitHub Actions OIDC tokens, eliminating the need for long-lived API keys
- A GitHub Actions workflow exchanges a short-lived OIDC token for a temporary, single-use NuGet API key
- No package author signing is needed — nuget.org automatically adds its own repository signature
- Docs: https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing
- Blog: https://andrewlock.net/easily-publishing-nuget-packages-from-github-actions-with-trusted-publishing/

**Step 1: Configure Trusted Publishing policy on nuget.org**
Go to https://www.nuget.org/account/trustedpublishing and create a policy:
- GitHub Repository Owner: `formfinch`
- Repository Name: the GitHub repo name for this project
- Workflow File: the filename chosen in Step 2 (e.g. `publish.yml`)
- Environment: leave blank (unless using GitHub environments)

**Step 2: Create GitHub Actions workflow file**
Create `.github/workflows/publish.yml` (or similar) that:
- Triggers on version tag push (e.g. `v*`)
- Sets permissions: `contents: read` and `id-token: write`
- Checks out the repo
- Sets up .NET SDK (8.0.x and 10.0.x to match the project's multi-targeting)
- Runs `dotnet pack -c Release`
- Uses `NuGet/login@v1` to exchange the OIDC token for a temporary NuGet API key
- Pushes `*.nupkg` files with `dotnet nuget push` using the temporary key to `https://api.nuget.org/v3/index.json`

**Step 3: Verify end-to-end**
- Push a test tag to trigger the workflow
- Confirm the package appears on nuget.org

**Important notes:**
- The workflow filename must exactly match what is registered in the nuget.org trusted publishing policy
- No API key secrets need to be stored in GitHub — the OIDC flow handles authentication
- The trusted publishing policy becomes permanently active after the first successful token exchange
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Trusted Publishing policy configured on nuget.org with correct repo owner, repo name, and workflow filename
- [ ] #2 GitHub Actions workflow file created in .github/workflows/ that triggers on version tag push
- [ ] #3 Workflow uses NuGet/login@v1 OIDC flow (no stored API keys)
- [ ] #4 Workflow builds and packs for both net8.0 and net10.0 targets
- [ ] #5 Package successfully published to nuget.org via the workflow
<!-- AC:END -->
