---
id: TASK-73
title: Add memory leak tests for runtime schema compilation (RuntimeValidatorFactory)
status: To Do
assignee: []
created_date: '2026-02-07 19:49'
labels:
  - testing
  - quality
  - memory
milestone: 1.0.0 Release
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Add memory leak tests covering the RuntimeValidatorFactory lifecycle in the Compiler project. The stress test project currently doesn't reference JsonSchemaValidation.Compiler, so a project reference needs to be added.

Key areas to test:
1. **Assembly unloading** — compiled schemas use AssemblyLoadContext; verify assemblies are collectible after disposal
2. **Cache lifecycle** — RuntimeValidatorFactory caches compiled validators by content hash; verify evicted/cleared entries are collectible
3. **Repeated compilation** — compiling many unique schemas shouldn't cause unbounded memory growth
4. **Factory disposal** — after disposing RuntimeValidatorFactory, all associated resources should be collectible

This was deferred from TASK-62 AC #4 because the stress test project didn't reference the Compiler project.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Add JsonSchemaValidation.Compiler project reference to stress test project
- [ ] #2 WeakReference test: compiled validator is collectible after factory disposal
- [ ] #3 WeakReference test: AssemblyLoadContext is collectible after unloading
- [ ] #4 Memory growth test: repeated compilation of unique schemas stays bounded
- [ ] #5 All tests pass on net8.0 and net10.0
<!-- AC:END -->
