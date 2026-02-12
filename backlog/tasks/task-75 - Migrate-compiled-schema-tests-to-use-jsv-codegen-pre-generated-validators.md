---
id: TASK-75
title: Migrate compiled schema tests to use jsv-codegen pre-generated validators
status: To Do
assignee: []
created_date: '2026-02-11 20:37'
labels:
  - testing
  - tooling
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Replace RuntimeValidatorFactory usage in CompiledSchemaValidationTests (all 6 drafts) and RuntimeValidatorFactoryTests with pre-generated compiled validators from jsv-codegen.

Use `jsv-codegen compile-test-schemas` to pre-generate validators from the JSON-Schema-Test-Suite submodule. Tests should reference the generated code instead of compiling schemas at runtime via Roslyn. Consider adding an MSBuild pre-build target or script to regenerate when the submodule is updated.
<!-- SECTION:DESCRIPTION:END -->
