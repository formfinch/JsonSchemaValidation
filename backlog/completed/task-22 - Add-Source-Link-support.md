---
id: TASK-22
title: Add Source Link support
status: Done
assignee: []
created_date: '2026-01-30 21:56'
updated_date: '2026-02-06 14:46'
labels:
  - nuget-package
  - debugging
milestone: 1.0.0 Release
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
- [x] #1 SourceLink package added
- [x] #2 <PublishRepositoryUrl>true</PublishRepositoryUrl> set
- [x] #3 <EmbedUntrackedSources>true</EmbedUntrackedSources> set
- [ ] #4 Verified: can debug into source from consuming project
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added Microsoft.SourceLink.GitHub with PrivateAssets=all. Set PublishRepositoryUrl and EmbedUntrackedSources. AC #4 (debug verification) deferred to post-publish. PR #7 merged.
<!-- SECTION:FINAL_SUMMARY:END -->
