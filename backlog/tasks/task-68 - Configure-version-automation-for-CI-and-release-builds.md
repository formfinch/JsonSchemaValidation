---
id: TASK-68
title: Configure version automation for CI and release builds
status: To Do
assignee: []
created_date: '2026-02-06 13:43'
labels:
  - infrastructure
  - nuget-package
milestone: 1.0.0 Release
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Update the project file versioning so CI and release workflows can set package versions correctly.

**Current state:**
The `.csproj` has `<Version>1.0.0</Version>` hardcoded. This doesn't support pre-release suffixes for CI builds.

**Required change:**
Replace `<Version>1.0.0</Version>` with `<VersionPrefix>1.0.0</VersionPrefix>` in `JsonSchemaValidation.csproj`. This allows:
- **Nightly CI:** `dotnet pack --version-suffix ci.{run_number}` → produces `1.0.0-ci.42`
- **Release:** `dotnet pack -p:Version={tag_version}` → produces `1.0.0` (version from git tag, overrides prefix entirely)

**Version flow:**
- Base version (`VersionPrefix`) is maintained in the `.csproj` and bumped manually per SemVer when preparing a release
- CI/nightly appends a suffix automatically
- Release workflow strips any suffix and uses the exact tag version

**No additional tooling needed** — this is pure MSBuild, no MinVer or other version management packages required.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 `<Version>` replaced with `<VersionPrefix>` in JsonSchemaValidation.csproj
- [ ] #2 `dotnet pack --version-suffix ci.1` produces a package versioned `1.0.0-ci.1`
- [ ] #3 `dotnet pack -p:Version=1.0.0` produces a package versioned `1.0.0`
- [ ] #4 No other version-related changes or tooling added
<!-- AC:END -->
