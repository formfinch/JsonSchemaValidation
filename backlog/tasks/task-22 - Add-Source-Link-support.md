---
id: TASK-22
title: Add Source Link support
status: To Do
assignee: []
created_date: '2026-01-30 21:56'
updated_date: '2026-01-31 10:50'
labels:
  - nuget-package
  - debugging
milestone: 'Phase 4: Package Configuration'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Add `Microsoft.SourceLink.GitHub` package to enable debugging into source code for package consumers.

```xml
<PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>
```
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 SourceLink package added
- [ ] #2 <PublishRepositoryUrl>true</PublishRepositoryUrl> set
- [ ] #3 <EmbedUntrackedSources>true</EmbedUntrackedSources> set
- [ ] #4 Verified: can debug into source from consuming project
<!-- AC:END -->
