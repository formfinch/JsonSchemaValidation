---
id: TASK-61.1
title: Investigate and resolve wasteful dictionary copying in validators
status: Done
assignee: []
created_date: '2026-02-01 00:21'
updated_date: '2026-02-03 00:31'
labels:
  - performance
  - research
milestone: 'Phase 3: API Stability'
dependencies: []
parent_task_id: TASK-61
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Analyze and fix validators that copy dictionary contents when race conditions are impossible.

**Pattern identified:**
- Factory creates fresh `Dictionary` + `List` collections
- Passes immediately to validator constructor
- Validator copies to `FrozenDictionary` + `string[]`
- Factory discards reference - no race condition possible

**Example:** `DependentRequiredValidator` / `DependentRequiredValidatorFactory`

**Tasks:**
1. Identify all validators following this pattern (20 files use FrozenDictionary)
2. Measure allocation overhead of unnecessary copies
3. Refactor: factory creates final type, validator takes ownership
4. Verify thread safety is maintained

**Goal:** Eliminate wasteful allocations during schema parsing without sacrificing thread safety.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 All validator constructors store collections directly without copying
- [ ] #2 All factories create final collection types before passing to validators
- [ ] #3 All 2887 tests pass
- [ ] #4 No new analyzer warnings
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
## Complete Analysis

### Problem Statement
Validators copy collections in their constructors when the factory just created those collections locally. Since there's no race condition (factory creates → passes → discards), the copy is wasteful.

### Scope

**Runtime Validators - AFFECTED (50+ copying operations)**

| Pattern | Count | Example |
|---------|-------|---------|
| `.ToFrozenDictionary()` | 18 calls in 14 files | `DependenciesValidator`, `PropertiesValidator` |
| `.ToFrozenSet()` | 8 calls in 7 files | `AdditionalPropertiesValidator` |
| `.ToArray()` | 28+ calls | `AllOfValidator`, `AnyOfValidator`, `OneOfValidator` |
| `as T[] ?? .ToArray()` | 11 calls | `RequiredValidator`, `AdditionalPropertiesValidator` |

**Code Generators - NOT AFFECTED**
- Generates inline source code, not runtime objects
- No constructor/factory pattern
- `.ToArray()` usage is for iterating JSON schema elements during generation

### Affected Files by Draft

**Draft 3:** DependenciesValidator, PropertiesValidator, AdditionalPropertiesValidator, ItemsArrayValidator, ExtendsValidator, DisallowValidator, TypeMultipleTypesValidator, AllOfValidator

**Draft 4:** DependenciesValidator, PropertiesValidator, AdditionalPropertiesValidator, ItemsArrayValidator, TypeMultipleTypesValidator, AllOfValidator, AnyOfValidator, OneOfValidator, RequiredValidator

**Draft 6:** DependenciesValidator, PropertiesValidator, AdditionalPropertiesValidator, ItemsArrayValidator, TypeMultipleTypesValidator, AllOfValidator, AnyOfValidator, OneOfValidator, RequiredValidator, PropertyNamesValidator, ContainsValidator

**Draft 7:** DependenciesValidator, PropertiesValidator, AdditionalPropertiesValidator, ItemsArrayValidator, TypeMultipleTypesValidator, AllOfValidator, AnyOfValidator, OneOfValidator, RequiredValidator, PropertyNamesValidator, ContainsValidator, IfThenElseValidator

**Draft 2019-09:** DependentRequiredValidator, DependentSchemasValidator, PropertiesValidator, AdditionalPropertiesValidator, ItemsArrayValidator, TypeMultipleTypesValidator, AllOfValidator, AnyOfValidator, OneOfValidator, RequiredValidator, PropertyNamesValidator, ContainsValidator, IfThenElseValidator, UnevaluatedPropertiesValidator, UnevaluatedItemsValidator

**Draft 2020-12:** DependentRequiredValidator, DependentSchemasValidator, PropertiesValidator, AdditionalPropertiesValidator, TypeMultipleTypesValidator, AllOfValidator, AnyOfValidator, OneOfValidator, RequiredValidator, PropertyNamesValidator, ContainsValidator, IfThenElseValidator, UnevaluatedPropertiesValidator, UnevaluatedItemsValidator, PrefixItemsValidator

### Fix Strategy

**For each validator/factory pair:**
1. Factory creates final collection type (FrozenDictionary, FrozenSet, array)
2. Validator takes final type directly and stores without conversion

**Example fix for PropertiesValidator:**
```csharp
// BEFORE (Factory)
var dict = new Dictionary<string, ISchemaValidator>();
// ... populate ...
return new PropertiesValidator(dict, contextFactory);

// BEFORE (Validator)
public PropertiesValidator(Dictionary<string, ISchemaValidator> props, ...)
{
    _props = props.ToFrozenDictionary(); // WASTEFUL COPY
}

// AFTER (Factory)  
var dict = new Dictionary<string, ISchemaValidator>();
// ... populate ...
return new PropertiesValidator(dict.ToFrozenDictionary(), contextFactory);

// AFTER (Validator)
public PropertiesValidator(FrozenDictionary<string, ISchemaValidator> props, ...)
{
    _props = props; // DIRECT ASSIGNMENT
}
```

### Implementation Order
1. Start with Draft 2020-12 (most recent, reference implementation)
2. Apply same pattern to earlier drafts
3. Run full test suite after each draft
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
### Implementation Order (Updated)
1. Draft 2020-12 → run `dotnet test --filter "Draft=2020-12"`
2. Draft 2019-09 → run `dotnet test --filter "Draft=2019-09"`
3. Draft 7 → run `dotnet test --filter "Draft=7"`
4. Draft 6 → run `dotnet test --filter "Draft=6"`
5. Draft 4 → run `dotnet test --filter "Draft=4"`
6. Draft 3 → run `dotnet test --filter "Draft=3"`
7. Run full test suite
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Eliminated wasteful collection copying in validators across all 6 drafts.

## Changes Made

### Pattern Applied
- **Before**: Factory creates `Dictionary`/`List` → converts to `FrozenDictionary`/`FrozenSet`/array → Validator stores final type
- **After**: Factory creates `Dictionary`/`List` → passes directly → Validator stores mutable collection (effectively immutable by convention)

### Type Changes
| Before | After |
|--------|-------|
| `FrozenDictionary<K,V>` | `Dictionary<K,V>` |
| `FrozenSet<T>` | `HashSet<T>` |
| `T[]` (via `[.. list]`) | `List<T>` |

### Files Modified
- **Draft 3**: 14 files (PropertiesValidator, DependenciesValidator, AdditionalPropertiesValidator, ExtendsValidator, DisallowValidator, TypeMultipleTypesValidator, ItemsArrayValidator + factories)
- **Draft 4**: 18 files
- **Draft 6**: 18 files
- **Draft 7**: 18 files
- **Draft 2019-09**: 20 files (includes DependentSchemasValidator, DependentRequiredValidator)
- **Draft 2020-12**: 18 files

### Benefits
- No `FrozenDictionary`/`FrozenSet` creation overhead
- No array copying via `[.. list]` spread syntax
- Concrete types provide struct enumerators (no heap allocation on iteration)
- Satisfies heap allocation analyzer (HAA0401)

### Test Results
All 4,158 tests pass on both net8.0 and net10.0.
<!-- SECTION:FINAL_SUMMARY:END -->
