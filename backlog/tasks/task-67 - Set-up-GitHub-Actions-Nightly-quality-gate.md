---
id: TASK-67
title: Set up GitHub Actions - Nightly quality gate
status: To Do
assignee: []
created_date: '2026-02-06 13:43'
labels:
  - infrastructure
  - ci-cd
  - quality
milestone: 1.0.0 Release
dependencies:
  - TASK-33
  - TASK-62
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Create a GitHub Actions workflow for extended quality validation that runs on a schedule — the thorough complement to the fast PR CI (TASK-33).

**Workflow triggers:**
- Scheduled (e.g., daily at 2:00 AM UTC), but only if main has new commits since last run
- Manual workflow dispatch (to produce a package artifact on demand)

**Jobs:**

1. **Build + unit tests** (same as PR CI — baseline sanity check)

2. **Security scanning**
   - `dotnet list package --vulnerable` — checks NuGet dependencies against known vulnerability database
   - Fail the workflow if vulnerable packages are found

3. **Memory leak tests** (TASK-62)
   - WeakReference-based GC collection tests
   - Memory growth detection over iterations
   - Covers: SchemaRepository, LruCache, Static API, compiled validators

4. **Stress / thread-safety tests**
   - Run the existing stress test project (`JsonSchemaValidationTests.Stress`)

5. **Performance benchmarks with baseline comparison**
   - Run BenchmarkDotNet suite
   - Compare results against committed baseline file in the repo
   - Fail or warn if any benchmark regresses beyond a defined threshold (e.g., >10% slower or >10% more allocations)
   - Baseline file is updated intentionally via a separate commit when performance characteristics change

6. **Produce downloadable artifact**
   - `dotnet pack` with pre-release version suffix (e.g., `1.0.0-ci.{run_number}`)
   - Upload `.nupkg` and `.snupkg` as GitHub Actions artifacts (downloadable for ~90 days)
   - This is the "grab the latest compiled package" mechanism

**Benchmark baseline approach:**
- Store baseline results as a JSON file committed to the repo (e.g., `benchmarks/baseline.json`)
- Nightly exports current results and compares against the baseline
- Options: `github-action-benchmark` action (supports BenchmarkDotNet), or custom comparison script
- When intentionally changing performance characteristics, update the baseline via a PR

**Design goals:**
- Thorough quality gate — catches regressions that the fast PR CI doesn't test for
- Cost-efficient — runs once per day max, skips if no changes
- Produces the latest pre-release package artifact for manual testing
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 `.github/workflows/nightly.yml` created
- [ ] #2 Scheduled trigger configured, skips if no new commits on main
- [ ] #3 Manual dispatch trigger available
- [ ] #4 `dotnet list package --vulnerable` runs and fails on vulnerable dependencies
- [ ] #5 Memory leak tests execute and report results
- [ ] #6 Stress/thread-safety tests execute and report results
- [ ] #7 Performance benchmarks run and compare against committed baseline
- [ ] #8 Benchmark regression beyond threshold fails the workflow
- [ ] #9 Pre-release `.nupkg` artifact uploaded (downloadable from Actions)
- [ ] #10 Version suffix uses CI run number (e.g., `1.0.0-ci.42`)
<!-- AC:END -->
