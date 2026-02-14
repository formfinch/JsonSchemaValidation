---
id: TASK-79
title: Support $vocabulary filtering in compiled validators
status: To Do
assignee: []
created_date: '2026-02-14 20:39'
labels:
  - compiled-validators
  - correctness
milestone: Compiled Validators
dependencies: []
priority: low
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Compiled validators currently include all keyword generators regardless of $vocabulary declarations in metaschemas. The dynamic validator correctly parses $vocabulary and filters keywords via VocabularyParser → SchemaMetadata.ActiveKeywords → factory filtering.

The compiler could read $vocabulary at code generation time using the existing VocabularyRegistry and filter keyword generators accordingly. This would make compiled validators respect custom metaschemas that restrict vocabularies (e.g., a metaschema declaring only applicator + core vocabularies should skip validation keywords like type, minimum, etc.).

Low priority — $vocabulary usage is rare (only 2 test cases in JSON-Schema-Test-Suite), but it's a correctness gap between dynamic and compiled validators.
<!-- SECTION:DESCRIPTION:END -->
