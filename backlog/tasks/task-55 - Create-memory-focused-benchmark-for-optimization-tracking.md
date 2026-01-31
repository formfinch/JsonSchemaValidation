---
id: TASK-55
title: Create memory-focused benchmark for optimization tracking
status: Done
assignee: []
created_date: '2026-01-30 21:58'
updated_date: '2026-01-31 10:51'
labels:
  - performance
  - benchmarks
  - tooling
milestone: 'Phase 9: Dynamic Validator Performance'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Create a dedicated memory allocation benchmark that measures FormFinch dynamic validator memory usage compared to competitors (LateApex, JsonSchema.Net). The benchmark serves as a feedback loop for optimization work - after each allocation reduction, run this benchmark to see the direct impact.

**Design goals:**
1. **Fast execution** - Complete in under 60 seconds so it can be run frequently during optimization work
2. **Memory-focused** - Primary metric is allocations (bytes), not execution time
3. **Keyword coverage** - Exercise all keywords that allocate memory, especially:
   - Property validators: `properties`, `patternProperties`, `additionalProperties`, `propertyNames`
   - Array validators: `items`, `prefixItems`, `additionalItems`, `contains`, `uniqueItems`
   - Applicators: `allOf`, `anyOf`, `oneOf`, `if`/`then`/`else`
   - References: `$ref`, `$dynamicRef`
   - Unevaluated: `unevaluatedProperties`, `unevaluatedItems` (FormFinch-specific overhead)
   - Dependencies: `dependentRequired`, `dependentSchemas`
   - Value validation: `enum`, `const`, `pattern`
4. **Competitor comparison** - Show FormFinch allocations relative to LateApex and JsonSchema.Net
5. **Trend visibility** - Output format that makes it easy to track allocation changes over time

**Implementation notes (2026-01-30):**
- Created `MemoryAllocationBenchmarks.cs` - Primary benchmark with `[Params(Simple, Medium, Complex)]` showing allocation scaling across 4 validators
- Created `CrossDraftMemoryBenchmarks.cs` - Cross-draft comparison, but **limited usefulness**: all drafts show identical allocations (26.35 KB) since they use the same common-feature schema. This benchmark doesn't provide actionable insights for optimization work. Consider removing or repurposing.
- Run commands: `dotnet run -c Release -- --filter *MemoryAllocation*` (primary), `--filter *CrossDraftMemory*` (cross-draft)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 MemoryAllocationBenchmarks.cs created with complexity scaling (Simple/Medium/Complex)
- [ ] #2 Benchmark completes in under 60 seconds (~82 seconds for 12 tests)
- [x] #3 Competitor comparison included (LateApex, JsonSchema.Net)
- [x] #4 Clear allocation metrics in output
- [x] #5 All allocating keywords represented in the test schema
- [ ] #6 Documented in benchmark methodology docs
- [ ] #7 Baseline measurements captured before optimization work
<!-- AC:END -->
