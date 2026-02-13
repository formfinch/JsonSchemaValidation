---
id: TASK-60
title: Add Copilot custom instructions
status: Done
assignee: []
created_date: '2026-01-31 22:57'
updated_date: '2026-02-13 23:02'
labels:
  - github
  - infrastructure
milestone: 'Phase 6: Release Infrastructure'
dependencies: []
priority: low
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Add custom instructions for GitHub Copilot pull request reviews at `.github/instructions/*.instructions.md`.

This enables smarter, more context-aware code reviews that understand the project's conventions, architecture, and standards.

Reference: https://docs.github.com/en/copilot/customizing-copilot/adding-repository-custom-instructions-for-github-copilot
<!-- SECTION:DESCRIPTION:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added `.github/copilot-instructions.md` with project-specific context for Copilot PR reviews, chat, and code generation. Covers architecture, code quality standards (6 analyzers), thread safety, naming conventions, performance guidelines, public API tracking, testing, and versioning. Addressed Copilot's own review feedback correcting the static field naming convention from `s_camelCase` to `PascalCase` to match actual codebase usage. Merged via PR #20.
<!-- SECTION:FINAL_SUMMARY:END -->
