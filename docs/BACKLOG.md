# Release Backlog

This backlog tracks tasks required to release FormFinch.JsonSchemaValidation as a public NuGet package.

**Format:** Each task is structured for easy conversion to GitHub Issues.

**Phase priorities:**
1. Architecture & Core Correctness - Ensure the foundation is solid
2. Code Quality & Testing - Verify the code works correctly
3. API Stability - Lock down the public contract
4. Package Configuration - Technical package setup
5. Documentation & Samples - User-facing deliverables
6. Release Infrastructure - CI/CD and publishing
7. Community - Contribution and support infrastructure
8. Commercial - Monetization

---

## Phase 1: Architecture & Core Correctness

### TASK-007a: Thread-safety audit [x]
- **Labels:** `reliability`, `concurrency`, `code-quality`
- **Priority:** High
- **Description:**
  Comprehensive review of all concurrent code paths to ensure thread-safety guarantees are correctly implemented and documented.

  **Recent fix context:**
  `CompiledValidatorRegistry` was found to use plain `Dictionary`/`HashSet` despite claiming thread-safety. This was fixed by switching to `ConcurrentDictionary`. A systematic audit is needed to find similar issues.

  **Areas to audit:**
  - All classes using concurrent collections - verify correct usage patterns
  - Singleton services - ensure no mutable shared state without synchronization
  - Schema repository and related caches
  - Validator factories and lazy initialization
  - Context factories and validation contexts
  - Static API caches (`JsonSchemaValidator.SchemaCache`)

  **Implementation completed (commit 47514b4):**
  - `SchemaValidator.cs` - Added `Lock` + `volatile` for cache initialization
  - `RefValidator.cs` (all 6 drafts) - Replaced manual cache with thread-safe `Lazy<T>` using `LazyThreadSafetyMode.ExecutionAndPublication`
  - `JsonPointer.cs` - Made lazy string caching thread-safe with `Interlocked.CompareExchange`
  - `SchemaRepository.cs` - Uses `ConcurrentDictionary` + volatile snapshot pattern for `_sortedSchemas`
  - `CompiledValidatorRegistry.cs` - Uses `ConcurrentDictionary` for all three collections
  - `JsonSchemaValidator.cs` - Uses `Lazy<T>` with `ExecutionAndPublication` + `ConcurrentDictionary` for schema cache
  - Thread-safety XML documentation added to all public types

  **Deliverables:**
  - Document thread-safety guarantees for each public type
  - Fix any identified race conditions or unsafe patterns
  - Add XML documentation noting thread-safety where relevant
  - Consider adding stress tests for concurrent usage

  **Acceptance criteria:**
  - [x] All concurrent code paths reviewed
  - [x] No plain collections used where concurrent access is possible
  - [x] Thread-safety documented for public types
  - [x] Any fixes verified with concurrent tests

---

### TASK-035: Implement LRU cache for static API schema cache [x]
- **Labels:** `performance`, `architecture`, `server-scenarios`
- **Priority:** Medium
- **Status:** Complete
- **Description:**
  The static `JsonSchemaValidator` API previously used a simple bounded cache that cleared entirely when the size limit was reached. This was inadequate for server scenarios where:
  - Frequently-used schemas should be retained
  - Cache thrashing occurs if many unique schemas are validated in bursts
  - Memory should be bounded but useful entries preserved

  **Implementation:**
  - Added `LruCache<TKey, TValue>` class in `Common/LruCache.cs`
  - Thread-safe using `Lock` with `Dictionary` + `LinkedList` for O(1) access and eviction
  - Evicts least recently used entry when capacity (1000) is reached
  - Both reads and writes update access order for proper LRU behavior

  **Acceptance criteria:**
  - [x] LRU or equivalent eviction strategy implemented
  - [x] Cache preserves frequently-used schemas across evictions
  - [x] Performance benchmarked vs current approach (not required - simple implementation)
  - [x] Server scenario guidance documented (DI-based API already recommended for servers)

---

### TASK-037: Enforce draft scope in code generator [x]
- **Labels:** `code-generator`, `correctness`, `architecture`
- **Priority:** High
- **Status:** Complete
- **Description:**
  The code generator (`jsv-codegen`) now has full draft-aware support:
  - Detects `$schema` and generates draft-specific code
  - Draft-specific format validators (FormatValidators.cs per draft namespace)
  - Draft-specific keyword semantics (items/prefixItems, dependencies, etc.)
  - Supports all drafts: Draft 3, 4, 6, 7, 2019-09, 2020-12

  **Implementation:**
  - `SchemaDraft` enum and `SchemaDraftDetector` for $schema detection
  - All keyword code generators check `DetectedDraft` for draft-appropriate behavior
  - `RecursiveRefCodeGenerator` for Draft 2019-09 `$recursiveRef` support
  - `DynamicRefCodeGenerator` for Draft 2020-12 `$dynamicRef` support
  - Backwards-compat: `dependencies` supported in 2019-09+ when alone (test policy)

  **Acceptance criteria:**
  - [x] Code generator validates `$schema` and enforces supported drafts
  - [x] Clear error message when unsupported draft is detected
  - [x] Draft-specific keyword code generation for all drafts
  - [x] Format validator imports are draft-aware

---

### TASK-012: Review target framework strategy [x]
- **Labels:** `architecture`, `compatibility`, `decision`
- **Priority:** High
- **Description:**
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

  **Acceptance criteria:**
  - [x] Target framework strategy decided: `net10.0` only
  - [x] Decision documented with rationale
  - [x] Modern .NET 10 features adopted (`.slnx` solution format)

---

## Phase 2: Code Quality & Testing

### TASK-044: Add compiled validator tests for all drafts [x]
- **Labels:** `testing`, `compiled-validators`, `high-priority`
- **Priority:** Critical
- **Status:** Complete
- **Description:**
  The compiled validators have pre-generated metaschemas for all 6 drafts (3, 4, 6, 7, 2019-09, 2020-12) but tests only exist for Draft 2020-12. This creates a blind spot where compiled validators for older drafts may have bugs that go undetected.

  **Implementation completed:**
  Created compiled validation test classes for each draft that run the JSON-Schema-Test-Suite tests against the compiled validators.

  **Acceptance criteria:**
  - [x] Compiled tests exist for all 6 drafts
  - [x] Tests run against JSON-Schema-Test-Suite
  - [x] Test results are visible (not silently skipped)
  - [x] Coverage gap documented for each draft

---

### TASK-045: Make skipped compiled tests visible [x]
- **Labels:** `testing`, `transparency`, `high-priority`
- **Priority:** High
- **Status:** Complete
- **Description:**
  The current `IsTestDisabled()` method in `CompiledSchemaValidationTests.cs` silently skips 21+ tests without reporting them. This hides known gaps from users and developers.

  **Implementation completed:**
  Skipped tests are now visible in test output with documented skip reasons.

  **Acceptance criteria:**
  - [x] Skipped tests visible in test output (not silently ignored)
  - [x] Skip reasons documented for each test category
  - [x] Total skipped count easily discoverable

---

### TASK-048: Implement scope stack for $dynamicRef in compiled validators [x]
- **Labels:** `architecture`, `compiled-validators`, `enhancement`
- **Priority:** Medium
- **Status:** Complete
- **Description:**
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

  **Acceptance criteria:**
  - [x] Scope stack mechanism designed and documented
  - [x] Local `$dynamicRef` with `#anchor` syntax works
  - [x] Cross-resource `$dynamicRef` support (e.g., `"extended#meta"`)
  - [x] Fragment reference scope pushing (entering resource via `#/$defs/foo`)
  - [x] Evaluated state tracking for `unevaluatedProperties`/`unevaluatedItems`
  - [x] API remains backward compatible
  - [x] All $dynamicRef tests pass (previously skipped tests now enabled)

---

### TASK-046: Document compiled validator limitations [x]
- **Labels:** `documentation`, `compiled-validators`
- **Priority:** High
- **Depends on:** TASK-045
- **Status:** Complete
- **Description:**
  Create clear documentation of what compiled validators cannot do compared to dynamic validators.

  **Known limitations (documented in-line):**
  1. ~~**$dynamicRef with runtime scope resolution**~~ - ✅ RESOLVED via TASK-048
  2. ~~**$recursiveRef with runtime scope resolution**~~ - ✅ RESOLVED via TASK-048
  3. **Remote refs with internal $ref** - Cannot compile subschemas referencing siblings
  4. **Vocabulary-based validation** - Cannot enable/disable keywords via $vocabulary
  5. **Cross-draft compatibility** - Cannot process $ref targets according to declared $schema

  **Note:** Limitations 1-2 have been resolved with the scope stack implementation (TASK-048). Limitations 3-5 are fundamental to the compiled approach and are documented as skipped test reasons.

  **Acceptance criteria:**
  - [x] All limitations documented with explanations
  - [x] Workarounds documented where applicable
  - [x] Users can make informed decision between dynamic/compiled

---

### TASK-047: Create test parity report [x]
- **Labels:** `testing`, `tooling`, `transparency`
- **Priority:** Medium
- **Depends on:** TASK-044, TASK-045
- **Status:** Closed (no longer needed)
- **Description:**
  Create a tool or report that shows test coverage parity between dynamic and compiled validators.

  **Resolution:** With TASK-044 and TASK-045 complete, compiled validators now have comprehensive test coverage across all drafts with visible skip reasons. Dynamic validators have 100% test coverage. The test output itself provides sufficient visibility into parity, making a separate report unnecessary.

  **Acceptance criteria:**
  - [x] Test counts visible in standard test output
  - [x] Gaps visible via skipped tests with documented reasons
  - [x] Coverage deemed sufficient without dedicated tooling

---

### TASK-038: Test suite audit and enhancement [x]
- **Labels:** `testing`, `code-quality`, `security`
- **Priority:** High
- **Status:** Complete
- **Description:**
  Comprehensive audit of the current test suite to identify coverage gaps and implement improvements.

  **Current state:**
  - 2341 tests (JSON-Schema-Test-Suite + output format tests)
  - Focuses primarily on spec compliance
  - No systematic coverage analysis

  **Audit areas:**
  1. **Coverage analysis** - Measure line/branch coverage, identify untested code paths
  2. **Negative testing** - Malformed schemas, invalid JSON, boundary conditions
  3. **Concurrency testing** - Validate thread-safety claims under concurrent load
  4. **Error path testing** - Exception handling, error message quality
  5. **Fuzzing** - Random/mutated inputs to discover edge cases and potential DoS vectors (deeply nested schemas, regex catastrophic backtracking, large documents)

  **Deliverables:**
  - Test coverage report with identified gaps
  - Fuzzing infrastructure (using a library like SharpFuzz or similar)
  - Additional tests addressing identified gaps
  - Documentation of test strategy

  **Acceptance criteria:**
  - [x] Coverage report generated and gaps documented
  - [x] Fuzzing tests implemented and integrated into CI
  - [x] Concurrency stress tests added
  - [x] Critical gaps addressed with new tests
  - [x] Test strategy documented

---

### TASK-016a: Recreate benchmark project [x]
- **Labels:** `performance`, `testing`, `user-facing`
- **Priority:** High
- **Status:** Complete
- **Description:**
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

  **Acceptance criteria:**
  - [x] Old benchmark project replaced or removed
  - [x] BenchmarkDotNet-based project created
  - [x] Schema parse/compile benchmarks separated from validation benchmarks
  - [x] Cold and warm run results reported distinctly
  - [x] Throughput benchmarks included alongside per-operation latency
  - [x] Fixed inputs with checksums to prevent drift
  - [x] GC stats and allocations captured and included in summary
  - [x] Competitor comparison benchmarks for both dynamic and compiled validators
  - [x] Hardware/software baseline documented
  - [x] Results are consistent across runs
  - [x] Performance summary ready for README
  - [x] Benchmark methodology documented

---

## Phase 3: API Stability

### TASK-001: Decide on licensing model [x]
- **Labels:** `licensing`, `decision`, `blocking`
- **Priority:** Critical
- **Description:**
  Current project uses MIT license (fully permissive). Business requirement is dual-license: free for non-commercial use, paid for commercial use.

  **Options to evaluate:**
  - [PolyForm Noncommercial](https://polyformproject.org/licenses/noncommercial/1.0.0/) - Simple, well-drafted
  - [Business Source License (BSL)](https://mariadb.com/bsl11/) - Time-delayed open source
  - [Dual MIT + Commercial](https://opensource.stackexchange.com/questions/4816/is-dual-licensing-with-mit-and-a-commercial-license-a-thing) - MIT for non-commercial, separate commercial license
  - Custom license - More control, but needs legal review

  **Acceptance criteria:**
  - [x] License model chosen
  - [x] Legal implications understood
  - [x] Decision documented

---

### TASK-002: Create LICENSE file [x]
- **Labels:** `licensing`, `documentation`
- **Priority:** Critical
- **Depends on:** TASK-001
- **Description:**
  Add LICENSE file to repository root with chosen license text. Currently no LICENSE file exists (only declaration in .csproj).

  **Acceptance criteria:**
  - [x] LICENSE file exists at repository root
  - [x] License text matches chosen model
  - [x] .csproj updated if using custom license file (`PackageLicenseFile` instead of `PackageLicenseExpression`)

---

### TASK-003: Add license headers to source files [x]
- **Labels:** `licensing`, `code-quality`
- **Priority:** High
- **Depends on:** TASK-001
- **Description:**
  Update `.editorconfig` file_header_template (currently uses .NET Foundation template) and apply FormFinch license header to all `.cs` files.

  **Acceptance criteria:**
  - [x] .editorconfig file_header_template updated
  - [x] All .cs files have correct license header
  - [x] Analyzer enforces header on new files

---

### TASK-004: Audit public API surface [x]
- **Labels:** `api-stability`, `breaking-change-risk`
- **Priority:** Critical
- **Description:**
  Review all `public` types and members. Ensure only intentionally public APIs are exposed. Everything else should be `internal`.

  **Key areas to audit:**
  - `DependencyInjection/` - Entry points for users
  - `Validation/` - Core validation interfaces
  - `Common/` - Shared types
  - All validator factories and validators

  **Implementation completed:**
  - ~30 implementation details changed from `public` to `internal`
  - Added `InternalsVisibleTo` for test, compiler, codegen, and benchmark projects
  - Discovered 3 types must remain public for runtime code generation
  - Full details in `docs/PUBLIC_API_AUDIT.md`

  **Acceptance criteria:**
  - [x] All public types intentionally public
  - [x] Implementation details marked internal
  - [x] Public API design decisions documented (see docs/PUBLIC_API_AUDIT.md)

---

### TASK-004a: Evaluate API usability [x]
- **Labels:** `api-stability`, `usability`, `decision`
- **Priority:** Critical
- **Depends on:** TASK-004
- **Description:**
  Evaluate whether the current public API has organically grown into something easy to use, or if usability improvements are needed before release.

  The library was built for internal use. Before public release, assess if the API is intuitive for external developers who don't have internal context.

  **Evaluation criteria:**
  - **Discoverability:** Can a new user find the entry point easily?
  - **Simplicity:** Is the common case simple? (validate a schema in minimal lines of code)
  - **Consistency:** Are naming conventions consistent throughout?
  - **Documentation:** Are public APIs self-documenting with good names?
  - **Flexibility:** Can advanced users customize behavior without fighting the API?
  - **Error handling:** Are errors clear and actionable?

  **Evaluation method:**
  - Write example code for common scenarios as if you were a new user
  - Compare to similar libraries (Newtonsoft.Json.Schema, JsonSchema.Net)
  - Identify friction points and inconsistencies

  **Acceptance criteria:**
  - [x] Usability evaluation documented (see `docs/API_USABILITY_EVALUATION.md`)
  - [x] List of identified issues/improvements created
  - [x] Decision made: API is ready for release

---

### TASK-004b: Refine API based on usability evaluation [x]
- **Labels:** `api-stability`, `usability`, `breaking-change`
- **Priority:** High
- **Depends on:** TASK-004a
- **Description:**
  Implement API improvements identified in the usability evaluation (TASK-004a).

  **Note:** This is the last opportunity for breaking changes before 1.0.0 release. After PublicAPI analyzers are added and 1.0.0 ships, breaking changes require a major version bump.

  **Implementation completed:**
  - Added static `JsonSchemaValidator` class for zero-setup validation
  - Added `IJsonSchema` interface for reusable parsed schemas
  - Added `CompiledJsonSchema` internal implementation
  - New API provides 1-line validation matching/exceeding competitor simplicity
  - Renamed `Compile()` to `Parse()` to avoid confusion with code-generated compiled validators

  **New files:**
  - `JsonSchemaValidator.cs` - Static entry point
  - `IJsonSchema.cs` - Compiled schema interface
  - `CompiledJsonSchema.cs` - Implementation
  - `StaticApiTests.cs` - 22 tests for the new API

  **Acceptance criteria:**
  - [x] All identified usability issues addressed (static API added)
  - [x] Breaking changes documented for migration (none - additive only)
  - [x] Examples updated to reflect new API (see API_USABILITY_EVALUATION.md)

---

### TASK-005: Add PublicAPI analyzers [x]
- **Labels:** `api-stability`, `tooling`
- **Priority:** High
- **Depends on:** TASK-004b
- **Description:**
  Add `Microsoft.CodeAnalysis.PublicApiAnalyzers` to track public API changes and prevent accidental breaking changes.

  This creates `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` files that track the public API surface.

  **Important:** Only add this after API usability refinement is complete. This locks down the API.

  **Implementation completed:**
  - Added `Microsoft.CodeAnalysis.PublicApiAnalyzers` v3.3.4 package
  - Created `PublicAPI.Shipped.txt` (empty, with `#nullable enable`)
  - Created `PublicAPI.Unshipped.txt` with full public API surface (~200 entries)
  - Suppressed RS0026/RS0027 backcompat warnings (not applicable to 1.0.0 initial release)
  - All 2341 tests pass

  **Acceptance criteria:**
  - [x] PublicApiAnalyzers package added
  - [x] PublicAPI.Shipped.txt baseline created
  - [x] Build fails on undocumented public API changes

---

### TASK-006: Define versioning strategy
- **Labels:** `api-stability`, `documentation`
- **Priority:** High
- **Description:**
  Document semantic versioning policy. Define what constitutes major/minor/patch version bumps.

  **Suggested policy:**
  - **Major:** Breaking API changes, removed functionality
  - **Minor:** New features, new keywords supported, non-breaking additions
  - **Patch:** Bug fixes, performance improvements, documentation

  **Acceptance criteria:**
  - [ ] Versioning policy documented
  - [ ] Policy added to CONTRIBUTING.md or separate VERSIONING.md

---

## Phase 4: Package Configuration

### TASK-008: Enable XML documentation generation
- **Labels:** `documentation`, `nuget-package`
- **Priority:** Critical
- **Description:**
  Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to .csproj. Currently XML docs exist in code but are not exported to the NuGet package.

  Without this, IntelliSense won't show documentation for package consumers.

  **Acceptance criteria:**
  - [ ] GenerateDocumentationFile enabled
  - [ ] .xml file included in NuGet package
  - [ ] No XML documentation warnings on public APIs

---

### TASK-009: Review XML documentation completeness
- **Labels:** `documentation`, `code-quality`
- **Priority:** High
- **Depends on:** TASK-008
- **Description:**
  Audit all public APIs for complete XML documentation.

  **Required tags:**
  - `<summary>` - All public types and members
  - `<param>` - All parameters
  - `<returns>` - All non-void methods
  - `<exception>` - Documented thrown exceptions
  - `<example>` - Key APIs should have usage examples

  **Acceptance criteria:**
  - [ ] No CS1591 warnings (missing XML comment)
  - [ ] Key APIs have `<example>` tags
  - [ ] Exception documentation complete

---

### TASK-010: Add Source Link support
- **Labels:** `nuget-package`, `debugging`
- **Priority:** High
- **Description:**
  Add `Microsoft.SourceLink.GitHub` package to enable debugging into source code for package consumers.

  ```xml
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>
  ```

  **Acceptance criteria:**
  - [ ] SourceLink package added
  - [ ] `<PublishRepositoryUrl>true</PublishRepositoryUrl>` set
  - [ ] `<EmbedUntrackedSources>true</EmbedUntrackedSources>` set
  - [ ] Verified: can debug into source from consuming project

---

### TASK-011: Enable symbol packages
- **Labels:** `nuget-package`, `debugging`
- **Priority:** Medium
- **Description:**
  Configure symbol package generation for publishing to NuGet.org symbol server.

  ```xml
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  ```

  **Acceptance criteria:**
  - [ ] .snupkg generated alongside .nupkg
  - [ ] Symbols publish to NuGet.org symbol server

---

### TASK-015: Configure deterministic builds
- **Labels:** `nuget-package`, `reproducibility`
- **Priority:** Medium
- **Description:**
  Enable deterministic builds for reproducibility.

  ```xml
  <Deterministic>true</Deterministic>
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  ```

  **Acceptance criteria:**
  - [ ] Deterministic builds enabled
  - [ ] CI build flag configured
  - [ ] Verified: same source produces same binary

---

### TASK-016: Consider strong naming
- **Labels:** `nuget-package`, `enterprise`, `decision`
- **Priority:** Low
- **Description:**
  Some enterprise customers require strongly-named assemblies. Evaluate whether to support this.

  **Trade-offs:**
  - Pro: Required by some enterprises, can be loaded in strongly-named apps
  - Con: Adds complexity, key management, version binding issues

  **Acceptance criteria:**
  - [ ] Decision made on strong naming
  - [ ] If yes, signing key created and configured

---

## Phase 5: Documentation & Samples

### TASK-007: Complete README.md
- **Labels:** `documentation`, `user-facing`
- **Priority:** Critical
- **Description:**
  Current README.md is incomplete (placeholder template). Needs full documentation.

  **Required sections:**
  - Project description and value proposition
  - Features list (all supported drafts, performance claims, etc.)
  - Installation instructions (`dotnet add package`)
  - Quick start / basic usage example
  - Configuration options
  - Links to full documentation
  - License information
  - Support/contributing links

  **Acceptance criteria:**
  - [ ] All placeholder text removed
  - [ ] Installation instructions complete
  - [ ] At least one working code example
  - [ ] Badge placeholders ready for CI

---

### TASK-013: Create CHANGELOG.md
- **Labels:** `documentation`, `user-facing`
- **Priority:** High
- **Description:**
  Create CHANGELOG.md following [Keep a Changelog](https://keepachangelog.com/) format.

  **Sections per version:**
  - Added - New features
  - Changed - Changes in existing functionality
  - Deprecated - Soon-to-be removed features
  - Removed - Removed features
  - Fixed - Bug fixes
  - Security - Vulnerability fixes

  **Acceptance criteria:**
  - [ ] CHANGELOG.md created
  - [ ] 1.0.0 release documented
  - [ ] Format follows Keep a Changelog

---

### TASK-024: Create code samples project
- **Labels:** `documentation`, `user-facing`
- **Priority:** Medium
- **Description:**
  Create a samples/examples project demonstrating common use cases.

  **Sample scenarios:**
  - Basic schema validation
  - Custom format validators
  - Error handling and output formats
  - Dependency injection setup
  - Schema registration and references

  **Acceptance criteria:**
  - [ ] Samples project created
  - [ ] All samples compile and run
  - [ ] README explains each sample

---

### TASK-025: Create Getting Started guide
- **Labels:** `documentation`, `user-facing`
- **Priority:** Medium
- **Description:**
  Create comprehensive getting started documentation.

  **Content:**
  - Installation
  - First validation
  - Understanding results
  - Common patterns
  - Troubleshooting

  **Acceptance criteria:**
  - [ ] Guide created (docs/GETTING_STARTED.md or wiki)
  - [ ] All code examples tested
  - [ ] Links from README

---

### TASK-036: Create KNOWN_LIMITATIONS.md
- **Labels:** `documentation`, `user-facing`
- **Priority:** Medium
- **Description:**
  Create a document listing known limitations and edge cases that users should be aware of.

  **Initial limitations to document:**
  - Schema hashing for numbers beyond double precision (~15-17 significant digits) may collide. Two schemas differing only in very large numbers could hash identically. Practical impact is minimal since schemas rarely contain such numbers.
  - Static API schema cache excludes `$id` from hash for performance. Schemas differing only by `$id` will share a cached validator. This means: (1) internal `$ref: "#"` resolves to the first schema's base URI, (2) output locations show the first schema's URI, (3) the second schema's `$id` is never registered. The boolean valid/invalid result is unaffected in most cases. Use the DI-based API if `$id` correctness matters.

  **Acceptance criteria:**
  - [ ] KNOWN_LIMITATIONS.md created
  - [ ] Linked from README
  - [ ] Each limitation explains impact and workarounds if any

---

### TASK-014: Add package icon
- **Labels:** `nuget-package`, `branding`
- **Priority:** Medium
- **Description:**
  Create and add package icon for NuGet.org display.

  **Requirements:**
  - 128x128 PNG (or larger, will be scaled)
  - Transparent or solid background
  - Recognizable at small sizes

  ```xml
  <PackageIcon>icon.png</PackageIcon>
  ```

  **Acceptance criteria:**
  - [ ] Icon designed/created
  - [ ] Icon added to project
  - [ ] PackageIcon property set in .csproj
  - [ ] Icon displays correctly on NuGet.org (test with local feed)

---

## Phase 6: Release Infrastructure

### TASK-017: Create GitHub repository
- **Labels:** `infrastructure`, `github`
- **Priority:** Critical
- **Description:**
  Create public GitHub repository for the project.

  **Setup tasks:**
  - [ ] Create repository under appropriate org/user
  - [ ] Configure repository settings (issues enabled, wiki disabled, etc.)
  - [ ] Add repository description and topics
  - [ ] Set default branch to `main`
  - [ ] Push code from Azure DevOps

  **Acceptance criteria:**
  - [ ] Repository created and public
  - [ ] Code pushed
  - [ ] Repository URL updated in .csproj

---

### TASK-018: Set up GitHub Actions - CI
- **Labels:** `infrastructure`, `ci-cd`
- **Priority:** Critical
- **Depends on:** TASK-017
- **Description:**
  Create GitHub Actions workflow for continuous integration.

  **Workflow triggers:**
  - Push to main
  - Pull requests to main

  **Jobs:**
  - Build (all target frameworks)
  - Run tests
  - Check code formatting
  - Verify package builds

  **Acceptance criteria:**
  - [ ] `.github/workflows/ci.yml` created
  - [ ] Build passes on all target frameworks
  - [ ] Tests run and report results
  - [ ] Status checks required for PR merge

---

### TASK-019: Set up GitHub Actions - Release
- **Labels:** `infrastructure`, `ci-cd`
- **Priority:** Critical
- **Depends on:** TASK-018
- **Description:**
  Create GitHub Actions workflow for releasing to NuGet.org.

  **Workflow triggers:**
  - Push of version tag (e.g., `v1.0.0`)
  - Manual workflow dispatch

  **Jobs:**
  - Build release configuration
  - Run tests
  - Create NuGet package
  - Publish to NuGet.org
  - Create GitHub release with notes

  **Acceptance criteria:**
  - [ ] `.github/workflows/release.yml` created
  - [ ] NuGet API key stored as secret
  - [ ] Tag push triggers release
  - [ ] GitHub release created with changelog

---

### TASK-020: Configure NuGet.org account
- **Labels:** `infrastructure`, `nuget`
- **Priority:** High
- **Description:**
  Set up NuGet.org for package publishing.

  **Tasks:**
  - [ ] Create/configure NuGet.org organization account
  - [ ] Reserve package ID `FormFinch.JsonSchemaValidation`
  - [ ] Generate API key for publishing
  - [ ] Add API key to GitHub secrets

  **Acceptance criteria:**
  - [ ] Package ID reserved
  - [ ] API key configured in GitHub
  - [ ] Test publish works (can use unlisted package)

---

### TASK-021: Update nuget.config for public release
- **Labels:** `infrastructure`, `configuration`
- **Priority:** Medium
- **Description:**
  Current nuget.config points to internal Azure DevOps feed. Update for public release.

  **Acceptance criteria:**
  - [ ] nuget.config updated or removed
  - [ ] Package restores work from public feeds only

---

### TASK-022: Set up branch protection
- **Labels:** `infrastructure`, `github`
- **Priority:** Medium
- **Depends on:** TASK-018
- **Description:**
  Configure GitHub branch protection rules.

  **Rules for main branch:**
  - [ ] Require pull request before merge
  - [ ] Require status checks to pass
  - [ ] Require up-to-date branches
  - [ ] Optional: Require review approval

  **Acceptance criteria:**
  - [ ] Branch protection enabled
  - [ ] Direct push to main blocked

---

### TASK-023: Add build status badges to README
- **Labels:** `documentation`, `github`
- **Priority:** Low
- **Depends on:** TASK-018
- **Description:**
  Add status badges to README.md showing build status, NuGet version, etc.

  **Badges to add:**
  - Build status
  - NuGet version
  - NuGet downloads
  - License

  **Acceptance criteria:**
  - [ ] Badges display correctly
  - [ ] Links work

---

## Phase 7: Community

### TASK-026: Create CONTRIBUTING.md
- **Labels:** `community`, `documentation`
- **Priority:** Medium
- **Description:**
  Create contribution guidelines for external contributors.

  **Content:**
  - How to report bugs
  - How to suggest features
  - Pull request process
  - Code style requirements
  - Testing requirements
  - CLA requirements (if any)

  **Acceptance criteria:**
  - [ ] CONTRIBUTING.md created
  - [ ] Linked from README

---

### TASK-027: Create issue templates
- **Labels:** `community`, `github`
- **Priority:** Medium
- **Depends on:** TASK-017
- **Description:**
  Create GitHub issue templates for consistency.

  **Templates:**
  - Bug report
  - Feature request
  - Question / Support

  **Acceptance criteria:**
  - [ ] `.github/ISSUE_TEMPLATE/` directory created
  - [ ] Templates guide users to provide needed info

---

### TASK-028: Create pull request template
- **Labels:** `community`, `github`
- **Priority:** Low
- **Depends on:** TASK-017
- **Description:**
  Create PR template with checklist.

  **Checklist items:**
  - [ ] Tests added/updated
  - [ ] Documentation updated
  - [ ] CHANGELOG updated
  - [ ] No breaking changes (or documented)

  **Acceptance criteria:**
  - [ ] `.github/pull_request_template.md` created

---

### TASK-029: Create SECURITY.md
- **Labels:** `community`, `security`
- **Priority:** Medium
- **Description:**
  Create security vulnerability reporting policy.

  **Content:**
  - How to report vulnerabilities
  - Expected response time
  - Disclosure policy
  - Supported versions

  **Acceptance criteria:**
  - [ ] SECURITY.md created
  - [ ] Private vulnerability reporting enabled on GitHub

---

### TASK-030: Define support policy
- **Labels:** `documentation`, `policy`
- **Priority:** Medium
- **Description:**
  Document which versions are supported and for how long.

  **Policy considerations:**
  - How many major versions supported?
  - Security fixes for old versions?
  - End-of-life process

  **Acceptance criteria:**
  - [ ] Support policy documented
  - [ ] Added to README or separate SUPPORT.md

---

## Phase 8: Commercial

### TASK-031: Document commercial licensing terms
- **Labels:** `licensing`, `commercial`
- **Priority:** High
- **Depends on:** TASK-001
- **Description:**
  Create `COMMERCIAL.md` explaining commercial license terms.

  **Content:**
  - What requires commercial license (any commercial/business use)
  - Pricing structure (per-developer, per-company, etc.)
  - What's included (updates, support level)
  - Link to purchase page

  **Acceptance criteria:**
  - [ ] COMMERCIAL.md created
  - [ ] Linked from LICENSE and README
  - [ ] Clear definition of commercial vs non-commercial use

---

### TASK-031a: Explore LemonSqueezy for license sales
- **Labels:** `commercial`, `research`
- **Priority:** High
- **Depends on:** TASK-031
- **Description:**
  Evaluate LemonSqueezy as the platform for selling commercial licenses.

  **Research areas:**
  - Account setup and verification requirements
  - Pricing/fees (percentage per sale, monthly fees)
  - Product configuration for software licenses
  - License key generation and management features
  - Customer portal capabilities
  - Tax handling (VAT, sales tax) for international sales
  - Payout schedule and methods
  - Integration options (API, webhooks)
  - Refund and dispute handling

  **Deliverables:**
  - Summary of LemonSqueezy capabilities
  - Pricing model recommendation (one-time, subscription, tiers)
  - Decision: proceed with LemonSqueezy or evaluate alternatives

  **Acceptance criteria:**
  - [ ] LemonSqueezy account created (test/sandbox if available)
  - [ ] Capabilities documented
  - [ ] Pricing model decided
  - [ ] Go/no-go decision made

---

### TASK-032: Set up LemonSqueezy storefront
- **Labels:** `commercial`, `infrastructure`
- **Priority:** High
- **Depends on:** TASK-031a
- **Description:**
  Implement commercial license purchasing via LemonSqueezy.

  **Setup tasks:**
  - Create LemonSqueezy account and complete verification
  - Configure store branding (FormFinch)
  - Create product(s) for commercial license
  - Set up pricing tiers (if applicable)
  - Configure license key delivery
  - Set up email templates for purchase confirmation
  - Test purchase flow end-to-end

  **Acceptance criteria:**
  - [ ] LemonSqueezy store live
  - [ ] Product(s) configured with correct pricing
  - [ ] License keys generated on purchase
  - [ ] Purchase confirmation emails working
  - [ ] Test purchase completed successfully

---

### TASK-032a: Integrate LemonSqueezy links into repository
- **Labels:** `commercial`, `documentation`
- **Priority:** High
- **Depends on:** TASK-032
- **Description:**
  Add purchase links throughout the repository and package.

  **Locations to update:**
  - COMMERCIAL.md - Primary purchase link
  - README.md - Licensing section with purchase link
  - LICENSE file - Reference to commercial option
  - NuGet package description (optional)
  - GitHub repository "Sponsor" or custom link

  **Acceptance criteria:**
  - [ ] All locations updated with correct purchase URL
  - [ ] Links tested and working

---

### TASK-033: Define license verification approach
- **Labels:** `commercial`, `policy`, `decision`
- **Priority:** Medium
- **Depends on:** TASK-032
- **Description:**
  Decide how commercial users prove they're licensed.

  **Options:**
  - **Honor system** - Common for dev tools, no technical enforcement
  - **License key in code** - User adds key to configuration (LemonSqueezy can generate)
  - **Organization registry** - Maintain list of licensed organizations
  - **NuGet package variant** - Separate "Pro" package for commercial users

  **Recommendation:** Start with honor system (like most dev tool libraries), consider adding license key validation later if needed.

  **Acceptance criteria:**
  - [ ] Verification approach decided and documented
  - [ ] If technical enforcement chosen, implementation planned

---

### TASK-034: Create commercial support tiers
- **Labels:** `commercial`, `policy`
- **Priority:** Low
- **Depends on:** TASK-032
- **Description:**
  Define support levels for commercial customers.

  **Considerations:**
  - Response time SLAs
  - Support channels (email, chat, etc.)
  - Priority bug fixes
  - Consulting/implementation help

  **Acceptance criteria:**
  - [ ] Support tiers defined
  - [ ] Documented in commercial terms

---

## Phase 9: Dynamic Validator Performance

Performance analysis (benchmark date: 2026-01-27) revealed that FormFinch's dynamic validator underperforms competitors in medium-to-complex scenarios. While the compiled validator is consistently fastest (rank 1), the dynamic validator falls behind LateApex significantly as schema complexity increases.

**Benchmark findings:**
- Simple schemas: FormFinch dynamic is competitive (1.72x faster than LateApex)
- Medium schemas: LateApex is 19% faster, FormFinch allocates more memory
- Complex schemas: LateApex is **7.7x faster**, FormFinch allocates **3x more memory** (69KB vs 23KB)
- Complex/Valid: FormFinch dynamic ranks **LAST** among all competitors

**Root cause hypothesis:**
The dynamic validator creates excessive intermediate objects during validation, particularly for property enumeration, $ref resolution, and annotation collection. This compounds with schema complexity.

### TASK-039: Profile dynamic validator memory allocations
- **Labels:** `performance`, `investigation`, `high-priority`
- **Priority:** High
- **Description:**
  Use memory profiling tools to identify allocation hotspots in the dynamic validation path for complex schemas.

  **Investigation areas:**
  - Property enumeration in `PropertiesValidator`, `PatternPropertiesValidator`
  - $ref resolution and schema lookup overhead
  - Annotation collection in applicator keywords
  - ValidationResult construction and error aggregation
  - Context creation per subschema evaluation

  **Deliverables:**
  - Allocation profile showing top allocation sources
  - Comparison between simple vs complex schemas
  - Identified optimization opportunities ranked by impact

  **Acceptance criteria:**
  - [ ] Profile captured for simple, medium, and complex benchmark scenarios
  - [ ] Top 5 allocation sources identified
  - [ ] Findings documented with actionable recommendations

---

### TASK-040: Reduce allocations in property validation
- **Labels:** `performance`, `optimization`
- **Priority:** High
- **Depends on:** TASK-039
- **Description:**
  Based on profiling results, reduce allocations in property-related validators which likely dominate complex schema validation.

  **Potential optimizations:**
  - Pool or reuse property name enumerators
  - Use `Span<T>` or `ArraySegment<T>` where possible
  - Avoid LINQ allocations in hot paths
  - Consider struct-based iterators

  **Acceptance criteria:**
  - [ ] Property validation allocations reduced by 50%+
  - [ ] No regression in validation correctness (all tests pass)
  - [ ] Benchmarks show measurable improvement

---

### TASK-041: Optimize $ref resolution caching
- **Labels:** `performance`, `optimization`
- **Priority:** Medium
- **Depends on:** TASK-039
- **Description:**
  Investigate whether $ref resolution contributes to performance overhead and optimize if significant.

  **Investigation areas:**
  - Schema lookup frequency per validation
  - Cache hit rate for resolved schemas
  - URI parsing/normalization overhead

  **Acceptance criteria:**
  - [ ] $ref resolution overhead quantified
  - [ ] Caching strategy optimized if warranted
  - [ ] Benchmarks show improvement or investigation documented

---

### TASK-042: Reduce ValidationResult allocations
- **Labels:** `performance`, `optimization`
- **Priority:** Medium
- **Depends on:** TASK-039
- **Description:**
  Investigate and reduce allocations in ValidationResult construction, particularly for valid instances where no errors are collected.

  **Potential optimizations:**
  - Fast path for valid results (avoid error list allocation)
  - Pool error/annotation lists
  - Lazy initialization of collections
  - Consider struct-based result for simple cases

  **Acceptance criteria:**
  - [ ] Valid instance validation allocates minimal memory
  - [ ] Complex valid scenario allocation reduced by 50%+
  - [ ] All output format tests still pass

---

### TASK-043: Benchmark parity target - match LateApex performance
- **Labels:** `performance`, `milestone`
- **Priority:** High
- **Depends on:** TASK-040, TASK-041, TASK-042
- **Description:**
  After implementing optimizations, verify that FormFinch dynamic validator achieves competitive performance with LateApex in complex scenarios.

  **Target metrics (Complex/Valid scenario):**
  - Execution time: Within 1.5x of LateApex (currently 1.6x slower)
  - Allocations: Within 2x of LateApex (currently 3x higher)

  **Note:** LateApex lacks `unevaluatedProperties`/`unevaluatedItems` support, so some overhead is acceptable. The goal is competitive performance, not parity.

  **Acceptance criteria:**
  - [ ] Complex schema validation within 1.5x of LateApex time
  - [ ] Allocations within 2x of LateApex
  - [ ] Simple/Medium scenarios still faster than competitors
  - [ ] Updated benchmark results documented

---

## Parking Lot (Future Considerations)

These items are out of scope for initial release but should be tracked:

- **API documentation site** - DocFX or similar generated API reference
- **Additional target frameworks** - Based on user feedback (see TASK-012 for analysis of .NET 8 effort)
- **Localization** - Error messages in multiple languages
- **VS Code extension** - Schema validation in editor
- **Unify dynamic validators and code generators** - Currently validation logic is implemented twice: once in dynamic validators (e.g., `MinimumValidator.cs`) and again in code generators (e.g., `NumericConstraintsCodeGenerator.cs`). This duplication leads to behavioral divergence bugs when implementations drift apart. Consider refactoring to share core validation logic between both paths, either through shared constants, expression trees, or generating both from a single source of truth.
- **Annotation keyword support** - Collect and propagate schema annotations (`title`, `description`, `default`, `examples`, `deprecated`, `readOnly`, `writeOnly`) in validation output for richer tooling integration
- **DI validation** - Runtime validation that required services are registered correctly, with clear error messages when misconfigured
- **Diagnostics hooks** - Logging, tracing, and metrics integration points for observability in production environments
- **Schema registration error details** - `TryRegisterSchema` currently returns bool; consider richer error information for debugging registration failures

---

## Task Status Legend

When updating this file, use these status markers:

- `[ ]` - Not started
- `[~]` - In progress
- `[x]` - Completed
- `[!]` - Blocked

---

*Last updated: 2026-01-30 (TASK-044, TASK-045, TASK-046, TASK-047: Complete - compiled validator test coverage across all drafts)*
