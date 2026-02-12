---
id: TASK-77
title: Delete JsonSchemaValidation.Compiler project
status: To Do
assignee: []
created_date: '2026-02-11 20:37'
updated_date: '2026-02-11 20:37'
labels:
  - cleanup
  - tooling
dependencies:
  - TASK-75
  - TASK-76
priority: low
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
After tests and benchmarks are migrated to pre-generated validators via jsv-codegen, the JsonSchemaValidation.Compiler project (containing RuntimeValidatorFactory) has no remaining consumers. Remove the project, its tests (RuntimeValidatorFactoryTests), related SkipReasons entries, and the solution/project references.
<!-- SECTION:DESCRIPTION:END -->
