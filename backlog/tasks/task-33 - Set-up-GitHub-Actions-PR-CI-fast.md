---
id: TASK-33
title: Set up GitHub Actions - PR CI (fast)
status: To Do
assignee: []
created_date: '2026-01-30 21:57'
updated_date: '2026-02-06 13:42'
labels:
  - infrastructure
  - ci-cd
milestone: 1.0.0 Release
dependencies:
  - TASK-32
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Create a fast GitHub Actions CI workflow for pull request validation.

**Workflow triggers:**
- Pull requests to main only (not push to main — saves Actions minutes on private repo)
- Path filters: skip runs for docs-only or backlog-only changes

**Jobs:**
- Build (all target frameworks: net8.0, net10.0)
- Run unit tests (JSON-Schema-Test-Suite tests only — no benchmarks, no memory leak tests, no stress tests)
- Verify package builds (`dotnet pack`)
- Static analysis runs automatically at build time via existing analyzers

**Design goals:**
- Fast feedback: target under 2-3 minutes
- Concurrency groups: cancel in-progress runs when new commits are pushed to the same PR
- Minimal cost: only runs on PRs, not on every push to main

**Out of scope (handled by nightly workflow):**
- Security scanning (`dotnet list package --vulnerable`)
- Memory leak tests
- Performance benchmarks
- Stress/thread-safety tests
- Artifact production (.nupkg uploads)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 `.github/workflows/ci.yml` created
- [ ] #2 Build passes on net8.0 and net10.0
- [ ] #3 Unit tests run and report results
- [ ] #4 Package build verified (`dotnet pack`)
- [ ] #5 Path filters exclude docs/backlog changes
- [ ] #6 Concurrency groups cancel superseded runs
- [ ] #7 Status checks required for PR merge (branch protection)
<!-- AC:END -->
