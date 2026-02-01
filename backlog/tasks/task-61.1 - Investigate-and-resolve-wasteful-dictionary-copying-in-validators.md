---
id: TASK-61.1
title: Investigate and resolve wasteful dictionary copying in validators
status: To Do
assignee: []
created_date: '2026-02-01 00:21'
updated_date: '2026-02-01 00:21'
labels:
  - performance
  - research
milestone: 'Phase 3: API Stability'
dependencies: []
parent_task_id: TASK-61
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Analyze and fix validators that copy dictionary contents when race conditions are impossible.

**Pattern identified:**
- Factory creates fresh `Dictionary` + `List` collections
- Passes immediately to validator constructor
- Validator copies to `FrozenDictionary` + `string[]`
- Factory discards reference - no race condition possible

**Example:** `DependentRequiredValidator` / `DependentRequiredValidatorFactory`

**Tasks:**
1. Identify all validators following this pattern (20 files use FrozenDictionary)
2. Measure allocation overhead of unnecessary copies
3. Refactor: factory creates final type, validator takes ownership
4. Verify thread safety is maintained

**Goal:** Eliminate wasteful allocations during schema parsing without sacrificing thread safety.
<!-- SECTION:DESCRIPTION:END -->
