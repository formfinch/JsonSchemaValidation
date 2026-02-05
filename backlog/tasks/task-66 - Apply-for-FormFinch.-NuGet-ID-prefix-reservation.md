---
id: TASK-66
title: Apply for FormFinch. NuGet ID prefix reservation
status: To Do
assignee: []
created_date: '2026-02-05 22:23'
labels:
  - nuget
  - infrastructure
dependencies:
  - TASK-35
documentation:
  - 'https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation'
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
After the first version of `FormFinch.JsonSchemaValidation` is published on nuget.org, apply for the `FormFinch.` package ID prefix reservation.

**What this gives us:**
- Verified checkmark badge on nuget.org for all `FormFinch.*` packages
- Prevents anyone else from publishing packages under the `FormFinch.*` prefix
- Visual trust indicator in Visual Studio

**How to apply:**
- Follow the process at https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation
- The NuGet team reviews applications based on criteria like having existing published packages and the prefix clearly identifying the owner
- Having at least one published `FormFinch.*` package strengthens the application

**Prerequisites:**
- At least one version of `FormFinch.JsonSchemaValidation` must be published first (TASK-35)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Prefix reservation application submitted to NuGet.org for FormFinch.*
- [ ] #2 Reservation approved and verified badge visible on the package page
<!-- AC:END -->
