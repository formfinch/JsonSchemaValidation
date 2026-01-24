# Release Backlog

This backlog tracks tasks required to release FormFinch.JsonSchemaValidation as a public NuGet package.

**Format:** Each task is structured for easy conversion to GitHub Issues.

---

## Phase 1: Foundation

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

## Phase 2: Package Quality

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

### TASK-012: Review target framework strategy
- **Labels:** `nuget-package`, `compatibility`, `decision`
- **Priority:** High
- **Description:**
  Currently targeting `net10.0` only. Consider broader compatibility.

  **Options:**
  - `net10.0` only - Smallest surface, latest features (current)
  - Add `net8.0` - LTS version, many users still on this
  - Add `netstandard2.0` - Maximum compatibility (.NET Framework, older .NET Core)

  **Trade-offs:**
  - More targets = more testing, potential API differences
  - Fewer targets = excludes users on older frameworks

  **Acceptance criteria:**
  - [ ] Target framework strategy decided
  - [ ] If multi-targeting, conditional compilation handled
  - [ ] All targets tested

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

### TASK-016a: Recreate benchmark project
- **Labels:** `performance`, `documentation`, `user-facing`
- **Priority:** High
- **Description:**
  The current benchmark project has inconsistent timing and no standardized benchmarking approach. Performance is a key selling point of the library, so benchmarks must be reliable and communicable to users.

  **Requirements:**
  - Use [BenchmarkDotNet](https://benchmarkdotnet.org/) for standardized, reliable measurements
  - Design benchmarks that demonstrate real-world performance scenarios
  - Results should be reproducible and comparable across runs
  - Output should be suitable for inclusion in README/documentation

  **Benchmark scenarios to include:**
  - Simple schema validation (type, required, properties)
  - Complex schema validation (nested, refs, allOf/anyOf/oneOf)
  - Large document validation
  - Schema compilation time vs validation time
  - Comparison across draft versions (if meaningful)
  - Memory allocation tracking

  **Deliverables:**
  - New benchmark project using BenchmarkDotNet
  - Documented benchmark methodology
  - Baseline results for 1.0.0 release
  - Instructions for running benchmarks

  **Acceptance criteria:**
  - [ ] Old benchmark project replaced or removed
  - [ ] BenchmarkDotNet-based project created
  - [ ] All key scenarios covered
  - [ ] Results are consistent across runs
  - [ ] Performance summary ready for README
  - [ ] Benchmark methodology documented

---

## Phase 3: Release Infrastructure

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

## Phase 4: Polish & Community

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

## Phase 5: Commercial

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

### TASK-035: Implement LRU cache for static API schema cache
- **Labels:** `performance`, `server-scenarios`
- **Priority:** Medium
- **Description:**
  The static `JsonSchemaValidator` API currently uses a simple bounded cache that clears entirely when the size limit is reached. This is inadequate for server scenarios where:
  - Frequently-used schemas should be retained
  - Cache thrashing occurs if many unique schemas are validated in bursts
  - Memory should be bounded but useful entries preserved

  **Recommended approach:**
  - Implement LRU (Least Recently Used) eviction
  - Consider using `System.Runtime.Caching.MemoryCache` or a lightweight LRU implementation
  - Evict oldest entries when limit reached instead of clearing all
  - Optionally make cache size configurable

  **Alternative considerations:**
  - Time-based expiration for rarely reused schemas
  - Weak references for GC-friendly caching
  - Document that DI-based API should be preferred for server scenarios

  **Acceptance criteria:**
  - [ ] LRU or equivalent eviction strategy implemented
  - [ ] Cache preserves frequently-used schemas across evictions
  - [ ] Performance benchmarked vs current approach
  - [ ] Server scenario guidance documented

---

### TASK-036: Create KNOWN_LIMITATIONS.md
- **Labels:** `documentation`, `user-facing`
- **Priority:** Medium
- **Description:**
  Create a document listing known limitations and edge cases that users should be aware of.

  **Initial limitations to document:**
  - Schema hashing for numbers beyond double precision (~15-17 significant digits) may collide. Two schemas differing only in very large numbers could hash identically. Practical impact is minimal since schemas rarely contain such numbers.
  - Static API schema cache uses simple clear-on-overflow strategy (see TASK-035 for improvement)
  - Static API schema cache excludes `$id` from hash for performance. Schemas differing only by `$id` will share a cached validator. This means: (1) internal `$ref: "#"` resolves to the first schema's base URI, (2) output locations show the first schema's URI, (3) the second schema's `$id` is never registered. The boolean valid/invalid result is unaffected in most cases. Use the DI-based API if `$id` correctness matters.

  **Acceptance criteria:**
  - [ ] KNOWN_LIMITATIONS.md created
  - [ ] Linked from README
  - [ ] Each limitation explains impact and workarounds if any

---

### TASK-037: Enforce draft scope in code generator
- **Labels:** `code-generator`, `correctness`, `benchmarking`
- **Priority:** High
- **Description:**
  The code generator (`jsv-codegen`) currently has ambiguous draft support:
  - Hardcodes `using FormFinch.JsonSchemaValidation.Draft202012.Keywords.Format;` for format validators
  - Has some cross-draft handling (`$id` vs `id`) but doesn't validate `$schema`
  - No enforcement or warning when generating validators for non-2020-12 schemas

  This is problematic because:
  1. **Benchmarking accuracy**: Benchmarks compare against validators like ajv using specific draft versions. If compiled validators don't properly implement those drafts, benchmarks are invalid.
  2. **Semantic correctness**: Different drafts have different keyword semantics (e.g., `items` in Draft 4 vs 2020-12).
  3. **User expectations**: Users may assume their Draft 7 schema will work correctly when compiled.

  **Required changes:**
  - Add `$schema` detection in code generator
  - Either:
    - (a) Restrict to Draft 2020-12 and 2019-09 only, with clear error for other drafts
    - (b) Implement proper draft-specific code generation for all supported drafts
  - Document supported drafts in code generator help/docs
  - Update format validator imports to be draft-aware (if supporting multiple drafts)

  **Note:** Internal metaschema validators (used to validate that user schemas conform to their draft) already exist for all drafts and are separate from user-facing code generation.

  **Acceptance criteria:**
  - [ ] Code generator validates `$schema` and enforces supported drafts
  - [ ] Clear error message when unsupported draft is detected
  - [ ] Documentation updated with supported draft list
  - [ ] Benchmarks only use schemas with supported drafts

---

## Parking Lot (Future Considerations)

These items are out of scope for initial release but should be tracked:

- **API documentation site** - DocFX or similar generated API reference
- **Additional target frameworks** - Based on user feedback
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

*Last updated: 2026-01-24 (TASK-001, TASK-002, TASK-003, TASK-004, TASK-004a, TASK-004b, TASK-005, TASK-007a completed)*
