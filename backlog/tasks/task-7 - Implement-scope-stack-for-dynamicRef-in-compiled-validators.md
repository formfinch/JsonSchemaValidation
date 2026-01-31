---
id: TASK-7
title: Implement scope stack for $dynamicRef in compiled validators
status: Done
assignee: []
created_date: '2026-01-30 21:54'
updated_date: '2026-01-31 10:51'
labels:
  - architecture
  - compiled-validators
  - enhancement
milestone: 'Phase 2: Code Quality & Testing'
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Implement runtime scope tracking to enable `$dynamicRef` resolution in compiled validators.

**Implementation completed (2026-01-28):**

**Phase 1 - Scope stack infrastructure:**
1. Created `ICompiledValidatorScope` interface for scope resolution
2. Created `IScopedCompiledValidator` interface extending `ICompiledValidator` with scope support
3. Created `CompiledScopeEntry` struct to hold anchor data in scope entries
4. Created `CompiledValidatorScope` immutable linked-list implementation with outermost-first search
5. Updated `SchemaCodeGenerator` to push scope entries and pass scope through validation calls
6. Updated `RefCodeGenerator` to pass scope to external refs via `IScopedCompiledValidator`

**Phase 2 - Resource-level anchor collection:**
7. Added `IsResourceRoot`, `ResourceAnchors`, and `ResourceRootHash` properties to `SubschemaInfo`
8. `SubschemaExtractor` tracks which anchors belong to which resource
9. Resource root schemas collect ALL `$dynamicAnchor` declarations within their resource

**Phase 3 - Cross-resource $dynamicRef support:**
10. `DynamicRefCodeGenerator` now handles cross-resource URIs like `"extended#meta"`
11. `GenerateExternalDynamicRefCode()` resolves external dynamic references with bookend checks
12. `GenerateLocalDynamicRefCallWithScope()` pushes resource anchors when entering via fragment ref

**Phase 4 - Evaluated state tracking:**
13. Created `IEvaluatedStateAwareCompiledValidator` interface to expose annotations
14. Created `EvaluatedStateSnapshot` for merging evaluated properties/items across external `$ref`
15. Enables `unevaluatedProperties`/`unevaluatedItems` to work correctly across external refs

**Tests now passing:**
- "A $dynamicRef resolves to the first $dynamicAnchor still in scope" ✓
- "A $dynamicRef that initially resolves to a schema with a matching $dynamicAnchor" ✓
- "multiple dynamic paths to the $dynamicRef keyword" ✓
- "after leaving a dynamic scope, it is not used by a $dynamicRef" ✓
- "$dynamicRef avoids the root of each schema, but scopes are still registered" ✓
- "$dynamicRef skips over intermediate resources" ✓
- "strict-tree schema, guards against misspelled properties" ✓

**Test results:** 3971 passed, 192 skipped, 0 failed (+10 tests from before implementation)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Scope stack mechanism designed and documented
- [ ] #2 Local $dynamicRef with #anchor syntax works
- [ ] #3 Cross-resource $dynamicRef support (e.g., "extended#meta")
- [ ] #4 Fragment reference scope pushing (entering resource via #/$defs/foo)
- [ ] #5 Evaluated state tracking for unevaluatedProperties/unevaluatedItems
- [ ] #6 API remains backward compatible
- [ ] #7 All $dynamicRef tests pass (previously skipped tests now enabled)
<!-- AC:END -->
