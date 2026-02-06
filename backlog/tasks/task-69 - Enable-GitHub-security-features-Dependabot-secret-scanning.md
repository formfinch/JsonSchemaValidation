---
id: TASK-69
title: Enable GitHub security features (Dependabot + secret scanning)
status: To Do
assignee: []
created_date: '2026-02-06 13:43'
labels:
  - infrastructure
  - security
milestone: 1.0.0 Release
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Enable the free GitHub security features available on private repos.

**Features to enable:**

1. **Dependabot alerts** (free on all repos)
   - Monitors NuGet dependencies for known vulnerabilities
   - Sends alerts when vulnerabilities are discovered
   - Enable in: GitHub repo → Settings → Code security → Dependabot alerts

2. **Dependabot security updates** (free on all repos)
   - Automatically creates PRs to update vulnerable dependencies
   - Enable in: GitHub repo → Settings → Code security → Dependabot security updates

3. **Secret scanning push protection** (free on all repos since 2024)
   - Blocks pushes that contain detected secrets (API keys, tokens, etc.)
   - Enable in: GitHub repo → Settings → Code security → Secret scanning

4. **Dependabot configuration** (optional)
   - Create `.github/dependabot.yml` to configure update schedule for NuGet packages
   - Can set to weekly security-only updates to avoid noise

**Not included (requires paid GHAS):**
- CodeQL static analysis — existing build-time analyzers (Meziantou, SonarAnalyzer, Roslynator, NetAnalyzers) cover similar ground
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Dependabot alerts enabled on the GitHub repository
- [ ] #2 Dependabot security updates enabled
- [ ] #3 Secret scanning push protection enabled
- [ ] #4 `.github/dependabot.yml` created with NuGet ecosystem configuration
<!-- AC:END -->
