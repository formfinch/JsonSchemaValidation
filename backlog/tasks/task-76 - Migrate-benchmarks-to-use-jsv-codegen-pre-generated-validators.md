---
id: TASK-76
title: Migrate benchmarks to use jsv-codegen pre-generated validators
status: To Do
assignee: []
created_date: '2026-02-11 20:37'
updated_date: '2026-02-13 22:02'
labels:
  - benchmarks
  - tooling
milestone: Compiled Validators
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Replace RuntimeValidatorFactory usage in benchmark projects (DotNetCompetitorBenchmarks, QuickCompetitorBenchmarks, LargeDocumentBenchmarks, MemoryAllocationBenchmarks) with pre-generated compiled validators from jsv-codegen.

This removes the Roslyn runtime dependency from benchmarks and dogfoods the same tool customers use.
<!-- SECTION:DESCRIPTION:END -->
