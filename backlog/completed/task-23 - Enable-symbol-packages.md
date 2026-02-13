---
id: TASK-23
title: Enable symbol packages
status: Done
assignee: []
created_date: '2026-01-30 21:56'
updated_date: '2026-02-06 15:04'
labels:
  - nuget-package
  - debugging
milestone: 1.0.0 Release
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Configure symbol package generation for publishing to NuGet.org symbol server.

```xml
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>
```
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 .snupkg generated alongside .nupkg
- [ ] #2 Symbols publish to NuGet.org symbol server
<!-- AC:END -->
