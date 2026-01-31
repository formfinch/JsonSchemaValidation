---
id: TASK-56
title: Quick allocation investigation - assess optimization potential
status: To Do
assignee: []
created_date: '2026-01-31 10:56'
labels:
  - performance
  - investigation
  - timeboxed
milestone: 'Phase 9: Dynamic Validator Performance'
dependencies: []
priority: medium
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
**Context:**
Dynamic validator performance is already competitive with (or better than) other .NET JSON Schema validators. Compiled validators exist for maximum performance scenarios. This investigation determines if further optimization of dynamic validators is warranted.

**Goal:**
Timeboxed investigation to identify if any "low-hanging fruit" optimizations exist - significant allocation reductions with minimal code complexity impact.

**Investigation steps:**
1. Run memory benchmark (`dotnet run -c Release -- --filter *MemoryAllocation*`)
2. Review allocation breakdown vs competitors
3. If FormFinch allocations are within 2x of best competitor: **close as "good enough"**
4. If significant gaps exist, identify top 1-2 hotspots
5. Assess: Can these be fixed without sacrificing code clarity?
   - If yes: implement and document
   - If no: close as "not worth the tradeoff"

**Decision criteria:**
- Code clarity > marginal performance gains
- Compiled validators handle perf-critical use cases
- Only implement optimizations that are obvious wins

**Outcome:**
Document findings and decision. Either implement easy wins or close with "current performance is sufficient" rationale.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Memory benchmark run and results captured
- [ ] #2 Allocation comparison vs competitors documented
- [ ] #3 Go/no-go decision made with rationale
- [ ] #4 If optimizations implemented: code clarity maintained, all tests pass
<!-- AC:END -->
