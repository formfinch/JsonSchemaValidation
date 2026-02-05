# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Mission

FormFinch.JsonSchemaValidation is a JSON Schema validation library for .NET with:
- **Full draft support:** Draft 3, Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
- **High performance:** Designed for speed
- **Pure System.Text.Json:** No external JSON dependencies
- **Public release goal:** NuGet package with source on GitHub

### Licensing Model

- **Non-commercial use:** Free
- **Commercial use:** Paid license required

This dual-license model supports open source adoption while sustaining development.

## Project Overview

FormFinch.JsonSchemaValidation is a .NET class library implementing JSON Schema validation using pure `System.Text.Json` (no external JSON dependencies). The library uses a plugin-based architecture with dependency injection for extensibility.

**Root Namespace:** `FormFinch.JsonSchemaValidation`
**Package ID:** `FormFinch.JsonSchemaValidation`

## Build Commands

```bash
# Build the solution
dotnet build

# Build specific configuration
dotnet build -c Release
```

## Running Tests

The project uses xUnit with 2887 tests covering all 6 JSON Schema drafts, output formats, thread safety, error handling, and compiled validators.

```bash
# Run all tests
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj

# Run specific test by name
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run tests for a specific draft
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj --filter "Trait=Draft&Trait=2020-12"

# Run with verbosity
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj -v normal
```

## Architecture

### Core Components

1. **Dependency Injection Layer** (`DependencyInjection/`)
   - `SchemaValidationSetup.cs` - Main DI configuration entry point
   - `SchemaValidationOptions` - Configuration (e.g., `FormatAssertionEnabled`)

2. **Schema Management** (`Repositories/`)
   - `SchemaRepository.cs` - Thread-safe schema registry using `ConcurrentDictionary`
   - Handles schema registration, URI resolution, and `$id` management

3. **Factory Pattern** (`Draft202012/Keywords/`)
   - 49 keyword-specific validator factories
   - Each keyword has a `*Validator.cs` implementing `IKeywordValidator` and a `*ValidatorFactory.cs` implementing `ISchemaDraftKeywordValidatorFactory`

4. **Validation Context** (`Common/`)
   - `IJsonValidationContext` - Encapsulates JSON values being validated
   - `ValidationScope` - Tracks scope for `$dynamicRef` resolution
   - `ScopeAwareSchemaValidator` - Manages dynamic scope during validation

5. **Validation Engine** (`Validation/`)
   - `SchemaValidator.cs` - Orchestrates keyword validators
   - `ValidationResult.cs` - Contains `IsValid`, `Errors`, and `Annotations`

### Keyword Categories in Draft 2020-12

- **Logic:** `AllOf`, `AnyOf`, `OneOf`, `Not`, `IfThenElse`
- **Applicator:** `Properties`, `Items`, `PrefixItems`, `PatternProperties`, `DependentSchemas`
- **Validation:** `Type`, `Enum`, `Const`, `Pattern`, `Format`, numeric constraints
- **Core:** `$ref`, `$dynamicRef`, `$anchor`, `$dynamicAnchor`, `$id`
- **Unevaluated:** `UnevaluatedItems`, `UnevaluatedProperties`

### Format Validators (`Draft202012/Keywords/Format/`)

19 format validators including: `date-time`, `date`, `time`, `email`, `hostname`, `ipv4`, `ipv6`, `uri`, `uuid`, etc. Features ECMAScript regex compatibility, IDN support, and leap second handling.

## Key Architectural Notes

1. **Validator Ordering**: Validators are sorted by `ExecutionOrder` property (default 0). `UnevaluatedItems` and `UnevaluatedProperties` use `ExecutionOrder = 100` to run last. This is enforced in `SchemaDraft202012ValidatorFactory` constructor.

2. **Dynamic Scope**: `$dynamicRef` and `$dynamicAnchor` require scope tracking via `ValidationScope` and `ScopeAwareSchemaValidator`.

3. **Remote Schemas**: Must be explicitly registered in `SchemaRepository` before use. Tests pre-load them from `submodules/JSON-Schema-Test-Suite/remotes/`.

4. **Thread Safety**: Schema registry uses `ConcurrentDictionary`; all validators registered as singletons.

## Test Infrastructure

- Tests use JSON-Schema-Test-Suite (git submodule at `submodules/JSON-Schema-Test-Suite/`)
- `TestCaseLoader.cs` dynamically loads test cases from JSON files
- Two service provider configurations: format annotation-only (default) and format assertion enabled

## Output Format Support

The library implements spec-compliant output formats per JSON Schema 2020-12 Section 12:

- **Flag** - Boolean only (most efficient)
- **Basic** - Flat list of errors with instance/keyword locations
- **Detailed** - Hierarchical nested structure with annotations

Key classes:
- `Validation/Output/OutputUnit.cs` - Spec-compliant output structure
- `Validation/Output/OutputFormat.cs` - Format enum
- `Common/JsonPointer.cs` - RFC 6901 JSON Pointer implementation

Usage:
```csharp
var output = validator.ValidateFlag(context);   // Just valid/invalid
var output = validator.ValidateBasic(context);  // Flat error list
var output = validator.ValidateDetailed(context); // Hierarchical
```

## Test Structure

Tests are organized by draft version and feature category:

```
JsonSchemaValidationTests/
├── Draft3/SchemaValidationTests.cs           # Draft 3 JSON-Schema-Test-Suite
├── Draft4/SchemaValidationTests.cs           # Draft 4 JSON-Schema-Test-Suite
├── Draft6/SchemaValidationTests.cs           # Draft 6 JSON-Schema-Test-Suite
├── Draft7/SchemaValidationTests.cs           # Draft 7 JSON-Schema-Test-Suite
├── Draft201909/SchemaValidationTests.cs      # Draft 2019-09 JSON-Schema-Test-Suite
├── Draft202012/
│   ├── SchemaValidationTests.cs              # Draft 2020-12 JSON-Schema-Test-Suite
│   ├── CompiledSchemaValidationTests.cs      # Compiled validator tests
│   ├── OutputFormat/                         # Output format tests
│   └── ThreadSafety/                         # Concurrency tests
├── Common/                                   # Shared validator tests
├── NegativeTests/                            # Malformed input & error message tests
├── ErrorPaths/                               # Exception handling tests
├── Compiler/                                 # Runtime compilation tests
├── ProductionSchemas/                        # Real-world schema tests
└── StaticApiTests.cs                         # Static API tests
```

Each draft's `SchemaValidationTests.cs` runs tests from the JSON-Schema-Test-Suite submodule with multiple service provider configurations (default, format assertion, content assertion where applicable).

## Known Gaps

None currently identified. Cross-draft compatibility is fully supported.

## Project Standards & Decisions

### Target Frameworks

**Current:** `net8.0;net10.0`

**Policy:**
- Support current LTS (.NET 8) and latest LTS (.NET 10)
- Evaluate adding net6.0 based on user demand (EOL but ~15% market share)
- No netstandard2.0 support (allows use of modern APIs)

**Rationale:** Multi-targeting net8.0 and net10.0 covers ~60% of the .NET market while keeping the codebase simple. APIs are nearly identical between these versions, requiring only minor adjustments (e.g., using `lock(object)` instead of the .NET 9+ `Lock` type).

### Versioning

**Strategy:** Semantic Versioning (SemVer)
- **Major:** Breaking API changes
- **Minor:** New features, backward compatible
- **Patch:** Bug fixes, performance improvements

### API Stability

- Public API surface is intentional and should remain stable
- Internal implementation details use `internal` access modifier
- Breaking changes require major version bump and migration guide

### Code Quality

**Required analyzers:** (all enforced at build time)
- Microsoft.CodeAnalysis.NetAnalyzers
- Roslynator.Analyzers
- SonarAnalyzer.CSharp
- Meziantou.Analyzer
- ClrHeapAllocationAnalyzer

**Build settings:**
- `TreatWarningsAsErrors: true`
- `EnforceCodeStyleInBuild: true`
- `Nullable: enable`

## Workflow Rules

1. **Plan before implementing:** Always present a plan before starting any implementation work. Wait for approval before coding.
2. **No commits without permission:** Do not commit changes without explicit permission from the user.
3. **Use Git Flow:** Never commit directly to `main`. Always create a feature branch, commit changes there, push, create a PR, and merge.


Follow the Karpathy Guidelines for coding agents

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

<!-- BACKLOG.MD MCP GUIDELINES START -->

<CRITICAL_INSTRUCTION>

## BACKLOG WORKFLOW INSTRUCTIONS

This project uses Backlog.md MCP for all task and project management activities.

**CRITICAL GUIDANCE**

- If your client supports MCP resources, read `backlog://workflow/overview` to understand when and how to use Backlog for this project.
- If your client only supports tools or the above request fails, call `backlog.get_workflow_overview()` tool to load the tool-oriented overview (it lists the matching guide tools).

- **First time working here?** Read the overview resource IMMEDIATELY to learn the workflow
- **Already familiar?** You should have the overview cached ("## Backlog.md Overview (MCP)")
- **When to read it**: BEFORE creating tasks, or when you're unsure whether to track work

These guides cover:
- Decision framework for when to create tasks
- Search-first workflow to avoid duplicates
- Links to detailed guides for task creation, execution, and finalization
- MCP tools reference

You MUST read the overview resource to understand the complete workflow. The information is NOT summarized here.

</CRITICAL_INSTRUCTION>

<!-- BACKLOG.MD MCP GUIDELINES END -->
