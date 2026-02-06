---
id: TASK-20
title: Enable XML documentation generation
status: To Do
assignee: []
created_date: '2026-01-30 21:56'
updated_date: '2026-02-06 13:43'
labels:
  - documentation
  - nuget-package
milestone: 1.0.0 Release
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to .csproj. Currently XML docs exist in code but are not exported to the NuGet package.

Without this, IntelliSense won't show documentation for package consumers.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 GenerateDocumentationFile enabled
- [ ] #2 .xml file included in NuGet package
- [ ] #3 No XML documentation warnings on public APIs
<!-- AC:END -->
