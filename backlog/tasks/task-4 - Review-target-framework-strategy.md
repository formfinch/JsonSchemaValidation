---
id: TASK-4
title: Review target framework strategy
status: Done
assignee: []
created_date: '2026-01-30 21:54'
updated_date: '2026-01-31 10:51'
labels:
  - architecture
  - compatibility
  - decision
milestone: 'Phase 1: Architecture & Core Correctness'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Currently targeting `net10.0` only. Consider broader compatibility.

**Options evaluated:**
- `net10.0` only - Smallest surface, latest features (current)
- Add `net8.0` - LTS version, supported until Nov 2026
- Add `netstandard2.0` - Maximum compatibility (.NET Framework, older .NET Core)

**Decision: Stay with `net10.0` only**

**Rationale:**
Experimental .NET 8 multi-targeting revealed significant blockers:
- `System.Threading.Lock` class (.NET 9+) - 1 file, requires conditional compilation
- C# 12 collection expressions - 2 files, trivial syntax changes
- `JsonElement.DeepEquals` (.NET 9+) - **28 files, 42 usages** - requires custom polyfill

The `JsonElement.DeepEquals` API is used pervasively for `enum`, `const`, and `uniqueItems` validation. Supporting .NET 8 would require:
- Writing ~100 lines of polyfill code
- Updating 22 source files
- Regenerating 6 compiled validators
- Ongoing maintenance burden

Additionally:
- .NET 8 support ends November 2026 (~10 months away)
- .NET 6 is already out of support (ended November 2024)
- Users adopting a new library should be on current LTS (.NET 10)
- Staying on .NET 10 allows use of modern features: `Lock`, `FrozenDictionary`, `JsonElement.DeepEquals`, collection expressions, `.slnx` solution format

Multi-targeting will be reconsidered if user demand materializes.

**Modernization completed:**
- Converted `JsonSchemaValidationSolution.sln` (124 lines) to `.slnx` format (11 lines)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Target framework strategy decided: net10.0 only
- [ ] #2 Decision documented with rationale
- [ ] #3 Modern .NET 10 features adopted (.slnx solution format)
<!-- AC:END -->
