---
id: TASK-58
title: Improve compiled validator code quality
status: To Do
assignee: []
created_date: '2026-01-31 11:14'
updated_date: '2026-02-12 21:09'
labels:
  - compiled-validators
  - code-quality
  - nice-to-have
  - future
dependencies: []
priority: low
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
**Context:**
The compiled validators generate working code, but the output is clearly machine-generated and could be improved for:
- Better readability/maintainability
- Potential performance gains from cleaner code patterns
- Easier debugging when users inspect generated code

**Current issues:**
- Generated code is verbose and repetitive
- Variable naming is mechanical (not human-friendly)
- Could use more idiomatic C# patterns
- Some redundant checks or suboptimal patterns

**Potential improvements:**
- Better variable naming conventions
- Consolidate repetitive patterns into helper methods
- Use more idiomatic C# (pattern matching, expression-bodied members)
- Optimize hot paths identified through profiling
- Consider source generators instead of runtime compilation (future)

**Note:** This is a nice-to-have improvement. The current compiled validators work correctly and are fast. Only pursue if there's user demand or clear performance wins.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Identify specific code quality issues in generated output
- [ ] #2 Implement improvements to code generator
- [ ] #3 Generated code is more readable and idiomatic
- [ ] #4 Performance is equal or better than before
- [ ] #5 All compiled validator tests pass
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
## Copilot Review Suggestions (PR #16)

Concrete improvements identified from Copilot review of `CompiledValidator_UserProfile.cs`:

1. **Combine redundant `if` blocks for required + property validation:** The generator emits separate `if (e.ValueKind == JsonValueKind.Object)` blocks — one for required property checks and another for property validation. These can be merged into a single block since the outer type check already guarantees `Object`.

2. **Eliminate redundant `ValueKind` guard after early return:** After `if (e.ValueKind != JsonValueKind.Object) return false;`, the subsequent `if (e.ValueKind == JsonValueKind.Object)` block is always true and can be removed (just emit the body directly).

3. **Use `.Where(...)` for explicit filtering in `uniqueItems` check:** The nested `foreach` loop in the uniqueItems validation implicitly filters — consider using LINQ `.Where(...)` or restructuring for clarity.

Source: https://github.com/formfinch/JsonSchemaValidation/pull/16
<!-- SECTION:NOTES:END -->
