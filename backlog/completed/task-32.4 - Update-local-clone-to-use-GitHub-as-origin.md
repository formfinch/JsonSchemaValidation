---
id: TASK-32.4
title: Update local clone to use GitHub as origin
status: Done
assignee: []
created_date: '2026-01-31 21:45'
updated_date: '2026-01-31 21:59'
labels:
  - github
  - infrastructure
dependencies:
  - TASK-32.3
parent_task_id: TASK-32
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Switch the primary remote from Azure DevOps to GitHub.

```bash
git remote set-url origin https://github.com/formfinch/JsonSchemaValidation.git
git remote remove github  # if added as separate remote
```

Verify push/pull works with new origin.
<!-- SECTION:DESCRIPTION:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Renamed Azure DevOps remote to `azure`, set GitHub as `origin`. Both branches now track origin (GitHub).
<!-- SECTION:FINAL_SUMMARY:END -->
