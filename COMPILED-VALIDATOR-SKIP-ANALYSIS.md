# Compiled Validator Skipped Tests Analysis

**Date:** 2026-01-29
**Current Status:** 88 tests skipped out of 1706 (94.8% pass rate)
**Target:** Enable ~80 tests to reach 99.5% pass rate

## Executive Summary

The 88 skipped compiled validator tests fall into 6 categories. Analysis shows that **80 tests can be enabled** with moderate effort, leaving only 8 tests as true architectural limitations.

| Category | Tests | Effort | Recommendation |
|----------|-------|--------|----------------|
| Remote refs with internal $ref | ~54 | Medium | Fix test setup |
| Base URI changes | ~15 | Combined with above | Fix test setup |
| unevaluated* with applicators (2019-09) | 7 | Medium | Enable annotation tracking |
| $recursiveRef (2019-09) | 4 | Low | Re-enable - infrastructure exists |
| Cross-draft | 3 | N/A | Skip - fundamental limitation |
| Vocabulary-based | 2 | N/A | Skip - fundamental limitation |

---

## Category 1: Remote Refs with Internal $ref (~54 tests)

### Problem Description

The test fixture's `ExtractSelfContainedSubschemas()` method skips any subschema containing `"$ref"`:

```csharp
// Current approach (Draft2020-12/CompiledSchemaValidationTests.cs:237)
if (!subschemaContent.Contains("\"$ref\"") && !subschemaContent.Contains("\"$dynamicRef\""))
{
    // Only then register the subschema
}
```

This is overly conservative. The compiler already handles internal `$ref` correctly.

### Affected Test Patterns

From test output analysis:
- "ref within remote ref" (6 tests)
- "root ref in remote ref" (5 tests)
- "relative pointer ref to array" (5 tests)
- "base URI change - change folder in subschema" (5 tests)
- "Location-independent identifier in remote ref" (5 tests)
- "retrieved nested refs resolve relative to their URI" (4 tests)
- "remote ref, containing refs itself" (4 tests)
- "ref overrides any sibling keywords" (4 tests)
- Plus ~16 more similar patterns

### Root Cause

Remote schemas like `subSchemas.json` contain internal `$ref`:

```json
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "$defs": {
        "integer": { "type": "integer" },
        "refToInteger": { "$ref": "#/$defs/integer" }  // This causes skip
    }
}
```

The current approach extracts `$defs/integer` but skips `$defs/refToInteger`.

### Solution

Instead of extracting subschemas individually, compile the **entire remote document** as a unit:

1. Remove the `Contains("$ref")` check in subschema extraction
2. Compile full remote documents with `RuntimeValidatorFactory`
3. Let the compiled validator's `RegisterSubschemas()` register all fragments
4. The two-phase initialization already handles cross-references

### Files to Modify

- `JsonSchemaValidationTests/Draft202012/CompiledSchemaValidationTests.cs`
- `JsonSchemaValidationTests/Draft201909/CompiledSchemaValidationTests.cs`
- `JsonSchemaValidationTests/Draft7/CompiledSchemaValidationTests.cs`
- `JsonSchemaValidationTests/Draft6/CompiledSchemaValidationTests.cs`
- `JsonSchemaValidationTests/Draft4/CompiledSchemaValidationTests.cs`
- `JsonSchemaValidationTests/Draft3/CompiledSchemaValidationTests.cs`

### Effort Estimate

4-6 hours

### Performance Impact

None at validation time. Slight increase in test setup time.

---

## Category 2: Base URI Changes (~15 tests)

### Problem Description

These tests overlap significantly with Category 1. When `$id` changes the base URI in a subschema, the test setup doesn't track this properly.

### Affected Test Patterns

- "Location-independent identifier with base URI change in subschema" (5 tests)
- "same $anchor with different base uri" (2 tests)
- "base URI change" (3 tests)
- "base URI change - change folder" (3 tests)
- "URN base URI with URN and anchor ref" (2 tests)

### Solution

The compiler handles this correctly via `SubschemaExtractor`'s resource tracking. The fix is in test setup:

1. Parse `$id` declarations during remote loading
2. Register validators by all declared `$id` URIs, not just file path
3. This is handled automatically when compiling full documents (Category 1 fix)

### Effort Estimate

Combined with Category 1 (no additional effort)

---

## Category 3: unevaluated* with Applicators - Draft 2019-09 Only (7 tests)

### Problem Description

These tests are skipped only in Draft 2019-09:
- "unevaluatedItems with anyOf"
- "unevaluatedItems with oneOf"
- "unevaluatedItems with if/then/else"
- "unevaluatedItems with $ref"
- "unevaluatedItems before $ref"
- "item is evaluated in an uncle schema to unevaluatedItems"
- "unevaluatedItems can see annotations from if without then/else"

### Root Cause

Draft 2020-12 passes these tests because the fixture uses `forceAnnotationTracking: true`:

```csharp
// Draft202012/CompiledSchemaValidationTests.cs:28
Factory = new RuntimeValidatorFactory(Registry, forceAnnotationTracking: true);

// Draft201909/CompiledSchemaValidationTests.cs:28
Factory = new RuntimeValidatorFactory(Registry, forceAnnotationTracking: false, defaultDraft: SchemaDraft.Draft201909);
```

### Solution

1. Test with `forceAnnotationTracking: true` for Draft 2019-09
2. If tests pass, enable conditionally when unevaluated* keywords are present
3. Remove the 7 skip conditions from `GetSkipReason()`

### Files to Modify

- `JsonSchemaValidationTests/Draft201909/CompiledSchemaValidationTests.cs`
  - Line 28: Change `forceAnnotationTracking: false` to `true`
  - Lines 510-524: Remove unevaluated skip conditions

### Effort Estimate

2-4 hours (including testing)

### Performance Impact

Minimal - annotation tracking adds ~5-10% overhead only when unevaluated* keywords are present.

---

## Category 4: $recursiveRef - Draft 2019-09 Only (4 tests)

### Problem Description

The `recursiveRef` test file is excluded from the test loader:

```csharp
// Draft201909/CompiledSchemaValidationTests.cs:393
// "recursiveRef" - excluded due to scope tracking complexity causing crashes (TASK-048)
```

Additionally, 4 specific tests are skipped:
- "multiple dynamic paths to the $recursiveRef keyword"
- "$ref with $recursiveAnchor"
- "unevaluatedItems with $recursiveRef"
- "unevaluatedProperties with $recursiveRef"

### Why This Should Work Now

TASK-048 is complete and $dynamicRef works. The $recursiveRef infrastructure is **identical**:

1. **`RecursiveRefCodeGenerator`** exists and generates proper scope-aware code:
   ```csharp
   // RecursiveRefCodeGenerator.cs:87
   sb.AppendLine($"if ({context.ScopeVariable}.TryResolveRecursiveAnchor(out var _recValidator_...))");
   ```

2. **`CompiledValidatorScope.TryResolveRecursiveAnchor`** is fully implemented (lines 74-97)

3. **`DynamicRefCodeGenerator`** already populates `HasRecursiveAnchor` and `RootValidator` in scope entries (line 416)

4. **Draft 2019-09 metaschemas** already use this successfully:
   - `CompiledValidator_Draft201909Schema.cs:280`
   - `CompiledValidator_Draft201909MetaCore.cs:224`
   - `CompiledValidator_Draft201909MetaContent.cs:108`
   - `CompiledValidator_Draft201909MetaApplicator.cs:160`

### Solution

1. Add `"recursiveRef"` to test loader list
2. Remove skip conditions for the 4 $recursiveRef tests
3. Run tests and fix any remaining issues

### Files to Modify

- `JsonSchemaValidationTests/Draft201909/CompiledSchemaValidationTests.cs`
  - Line 393: Add `"recursiveRef",` to test loader
  - Lines 451-461: Remove recursiveRefTests skip conditions

### Effort Estimate

~1 hour

### Performance Impact

None - uses existing infrastructure.

---

## Category 5: Cross-Draft Compatibility (3 tests)

### Problem Description

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$ref": "http://localhost:1234/draft2019-09/ignore-prefixItems.json"
}
```

When a 2020-12 schema references a 2019-09 schema, the validator must:
1. Detect the target's `$schema`
2. Apply 2019-09 rules to that target
3. Interpret keywords according to 2019-09 (e.g., ignore `prefixItems`)

### Affected Tests

- "refs to historic drafts are processed as historic drafts" (2 tests)
- "refs to future drafts are processed as future drafts" (1 test)

### Why This Cannot Be Fixed

Compiled validators generate validation code at compile time based on a single draft version. They cannot:
- Detect target schema's `$schema` at runtime
- Switch keyword interpretation dynamically
- Compile different code paths for different draft versions

### Recommendation

**Skip permanently** - This is a fundamental architectural limitation. Cross-draft compatibility requires runtime schema detection which contradicts compile-time code generation.

---

## Category 6: Vocabulary-Based Validation (2 tests)

### Problem Description

Tests schemas that use `$vocabulary` to disable validation keywords:

```json
{
  "$schema": "http://localhost:1234/.../metaschema-no-validation.json",
  "minimum": 10  // Should be ignored because validation vocabulary disabled
}
```

### Affected Tests

- "schema that uses custom metaschema with with no validation vocabulary" (2 tests)

### Why This Cannot Be Fixed

Compiled validators generate validation code at compile time. They cannot:
- Parse metaschema `$vocabulary` declarations at runtime
- Dynamically enable/disable keyword processing
- Generate conditional code based on vocabulary presence

### Recommendation

**Skip permanently** - This is a fundamental architectural limitation.

---

## Implementation Plan

### Phase 1: Quick Win - $recursiveRef (~1 hour)

**Goal:** Validate that the scope infrastructure works for $recursiveRef

1. Edit `Draft201909/CompiledSchemaValidationTests.cs`:
   - Add `"recursiveRef",` to test loader (after line 393)
   - Remove lines 451-461 (recursiveRefTests skip logic)

2. Run tests:
   ```bash
   dotnet test --filter "Draft=2019-09&Validator=Compiled"
   ```

3. Fix any failures

**Expected result:** 4 tests enabled

### Phase 2: Annotation Tracking for 2019-09 (~2-4 hours)

**Goal:** Enable unevaluated* tests for Draft 2019-09

1. Edit `Draft201909/CompiledSchemaValidationTests.cs`:
   - Line 28: Change `forceAnnotationTracking: false` to `true`
   - Remove unevaluated skip conditions (lines 510-524)

2. Run tests and verify

**Expected result:** 7 tests enabled

### Phase 3: Remote Schema Loading Fix (~4-6 hours)

**Goal:** Enable tests involving remote schemas with internal $ref

1. Refactor `LoadRemoteSchemas()` in all 6 draft test files:
   - Compile entire remote documents instead of extracting subschemas
   - Remove `ExtractSelfContainedSubschemas()` method
   - Let `RegisterSubschemas()` handle fragment registration

2. Update `GetSkipReason()` methods:
   - Remove `RemoteRefWithInternalRef` skip conditions
   - Remove `BaseUriChange` skip conditions

3. Run full test suite and fix any failures

**Expected result:** ~69 tests enabled

### Phase 4: Validation and Cleanup

1. Run full compiled validator test suite
2. Verify no performance regressions with benchmark
3. Update skip reason documentation
4. Clean up unused `SkipReasons` constants

---

## Performance Considerations

### What "Significant" Means

| Impact Level | Definition |
|--------------|------------|
| None | No measurable change in validation ops/sec |
| Minimal | <5% regression in common cases |
| Moderate | 5-15% regression in affected cases |
| Significant | >15% regression OR architectural changes affecting hot paths |

### Expected Impact by Phase

- Phase 1 ($recursiveRef): None - uses existing infrastructure
- Phase 2 (annotation tracking): Minimal - only affects schemas with unevaluated* keywords
- Phase 3 (remote loading): None at validation time (test setup only)

---

## Files Reference

### Test Files

| Draft | File |
|-------|------|
| 2020-12 | `JsonSchemaValidationTests/Draft202012/CompiledSchemaValidationTests.cs` |
| 2019-09 | `JsonSchemaValidationTests/Draft201909/CompiledSchemaValidationTests.cs` |
| 7 | `JsonSchemaValidationTests/Draft7/CompiledSchemaValidationTests.cs` |
| 6 | `JsonSchemaValidationTests/Draft6/CompiledSchemaValidationTests.cs` |
| 4 | `JsonSchemaValidationTests/Draft4/CompiledSchemaValidationTests.cs` |
| 3 | `JsonSchemaValidationTests/Draft3/CompiledSchemaValidationTests.cs` |

### Skip Reasons

- `JsonSchemaValidationTests/Common/SkipReasons.cs`

### Code Generation

- `JsonSchemaValidation.CodeGeneration/Keywords/RecursiveRefCodeGenerator.cs`
- `JsonSchemaValidation.CodeGeneration/Keywords/DynamicRefCodeGenerator.cs`
- `JsonSchemaValidation.CodeGeneration/Keywords/RefCodeGenerator.cs`
- `JsonSchemaValidation.CodeGeneration/Generator/SchemaCodeGenerator.cs`

### Runtime Infrastructure

- `JsonSchemaValidation/CompiledValidators/CompiledValidatorScope.cs`
- `JsonSchemaValidation/Abstractions/ICompiledValidatorScope.cs`
- `JsonSchemaValidation.Compiler/RuntimeValidatorFactory.cs`

---

## Success Metrics

| Metric | Before | After |
|--------|--------|-------|
| Tests Passed | 1618 | 1698 |
| Tests Skipped | 88 | 8 |
| Pass Rate | 94.8% | 99.5% |

### Permanently Skipped Tests (8)

- Cross-draft compatibility (3 tests)
- Vocabulary-based validation (2 tests)
- Edge cases TBD (3 tests)
