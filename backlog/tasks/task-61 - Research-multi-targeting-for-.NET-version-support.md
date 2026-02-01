---
id: TASK-61
title: Research multi-targeting for .NET version support
status: In Progress
assignee: []
created_date: '2026-01-31 23:13'
updated_date: '2026-02-01 00:23'
labels:
  - research
  - nuget
  - breaking-change
milestone: 'Phase 3: API Stability'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Investigate supporting multiple .NET versions in the NuGet package.

**Current state:** Targets only `net10.0`

**Questions to answer:**
1. Which .NET versions should be supported? (net8.0 LTS, net6.0, netstandard2.0/2.1?)
2. What API usage would break on older frameworks?
3. What is the performance impact of targeting older frameworks?
4. What is the build/test matrix complexity?
5. What do comparable libraries target? (e.g., json-everything, JsonSchema.Net)

**Outcome:** Recommendation on target frameworks for 1.0 release.
<!-- SECTION:DESCRIPTION:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
## Decision

**Target frameworks:** `net10.0` + `netstandard2.0`

**Rationale:**
- net10.0: Current LTS, best performance
- netstandard2.0: Broadest compatibility (.NET Framework 4.6.1+, .NET Core 2.0+)

**Constraints:**
- No PolySharp or similar polyfill packages
- Minimize external dependencies
- Use conditional compilation (`#if`) or code rewrites

**Implementation approach:**
1. Add `netstandard2.0` to TargetFrameworks
2. Create abstraction layer for FrozenDictionary (single `#if` location)
3. Replace collection expressions `[]` with `Array.Empty<T>()`
4. Replace range syntax with traditional methods
5. Handle `AsSpan` with `#if` or alternative implementations
<!-- SECTION:NOTES:END -->
