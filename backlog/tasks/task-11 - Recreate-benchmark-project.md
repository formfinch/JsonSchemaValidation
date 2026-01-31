---
id: TASK-11
title: Recreate benchmark project
status: Done
assignee: []
created_date: '2026-01-30 21:55'
updated_date: '2026-01-31 10:51'
labels:
  - performance
  - testing
  - user-facing
milestone: 'Phase 2: Code Quality & Testing'
dependencies: []
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Created a new BenchmarkDotNet-based benchmark project (`JsonSchemaValidation.Benchmarks`) replacing the old CLI benchmark tool.

**Implementation:**
- BenchmarkDotNet 0.14.0 with pinned dependency versions for reproducibility
- Compares FormFinch (dynamic & compiled) against JsonSchema.Net 7.4.0 and LateApex 2.1.6
- Embedded test data with SHA-256 checksum verification
- Schema complexity levels: Simple, Medium, Complex, Production

**Benchmark categories implemented:**
1. **Parsing benchmarks** - Cold (first-run) and warm (cached)
2. **Validation benchmarks** - Simple, complex, and large document
3. **Throughput benchmarks** - Batch validation with OperationsPerInvoke
4. **Cross-draft benchmarks** - Draft 4, 6, 7, 2019-09, 2020-12
5. **Competitor benchmarks** - FormFinch dynamic, FormFinch compiled, JsonSchema.Net, LateApex

**Note:** Ajv (Node.js) comparison was removed due to cross-platform module resolution issues. Benchmarks now focus exclusively on .NET packages.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Old benchmark project replaced or removed
- [ ] #2 BenchmarkDotNet-based project created
- [ ] #3 Schema parse/compile benchmarks separated from validation benchmarks
- [ ] #4 Cold and warm run results reported distinctly
- [ ] #5 Throughput benchmarks included alongside per-operation latency
- [ ] #6 Fixed inputs with checksums to prevent drift
- [ ] #7 GC stats and allocations captured and included in summary
- [ ] #8 Competitor comparison benchmarks for both dynamic and compiled validators
- [ ] #9 Hardware/software baseline documented
- [ ] #10 Results are consistent across runs
- [ ] #11 Performance summary ready for README
- [ ] #12 Benchmark methodology documented
<!-- AC:END -->
