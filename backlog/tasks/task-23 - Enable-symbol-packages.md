---
id: TASK-23
title: Enable symbol packages
status: To Do
assignee: []
created_date: '2026-01-30 21:56'
updated_date: '2026-01-31 10:50'
labels:
  - nuget-package
  - debugging
milestone: 'Phase 4: Package Configuration'
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
