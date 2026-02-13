# Copilot Instructions

## Project Overview

FormFinch.JsonSchemaValidation is a .NET JSON Schema validation library built on System.Text.Json. Primary focus is Draft 2020-12 with backward compatibility for Drafts 2019-09, 7, 6, 4, and 3. Targets net8.0 and net10.0. Dual-licensed: free for non-commercial use, paid license for commercial use.

## Architecture

- **DI-based plugin architecture** using Microsoft.Extensions.DependencyInjection
- **Factory pattern** for keywords: each keyword has a `*Validator.cs` (implements `IKeywordValidator`) and a `*ValidatorFactory.cs` (implements `ISchemaDraftKeywordValidatorFactory`)
- **Validator ordering** via `ExecutionOrder` property (default 0). `UnevaluatedItems` and `UnevaluatedProperties` use `ExecutionOrder = 100` to run last — this is critical for correctness
- **Dynamic scope tracking** via `ValidationScope` and `ScopeAwareSchemaValidator` for `$dynamicRef`/`$dynamicAnchor` resolution
- **Thread-safe schema registry** using `ConcurrentDictionary` in `SchemaRepository`
- All validators are registered as singletons — they must be stateless

## Code Quality Standards

Six analyzers enforced at build time with `TreatWarningsAsErrors`:
- Microsoft.CodeAnalysis.NetAnalyzers
- Roslynator.Analyzers
- SonarAnalyzer.CSharp
- Meziantou.Analyzer
- ClrHeapAllocationAnalyzer
- Microsoft.CodeAnalysis.PublicApiAnalyzers

Intentionally suppressed rules:
- **S3267** (simplify loops with LINQ): validators use loops with early returns for performance; LINQ would require full enumeration
- **MA0158** (use System.Threading.Lock): `lock(object)` used for net8.0/net10.0 multi-target compatibility (Lock type requires .NET 9+)
- **RS0026/RS0027** (PublicAPI compat warnings): suppressed for initial 1.0.0 release only

Nullable reference types are enabled. Code style is enforced in build (`EnforceCodeStyleInBuild`).

## Thread Safety

- Schema registry uses `ConcurrentDictionary`
- All validators are singletons — no mutable instance state
- Do not introduce mutable shared state without proper synchronization

## Naming Conventions

- `_camelCase` for private/internal fields
- `s_camelCase` for private/internal static fields
- `PascalCase` for constants, properties, methods
- Allman brace style, `using` directives outside namespace

## Performance Guidelines

- Prefer loops over LINQ in validation hot paths (enables early returns)
- Be aware of heap allocations — ClrHeapAllocationAnalyzer is active
- Validator `ExecutionOrder` matters: unevaluated keywords must run last
- Schema resolution and `$ref` traversal should avoid redundant lookups

## Public API

- Tracked by `Microsoft.CodeAnalysis.PublicApiAnalyzers`
- Changes to public surface must update `PublicAPI.Unshipped.txt`
- Types are `internal` by default — only expose what's intentional
- Breaking changes require a major version bump (SemVer)

## Testing

- xUnit with ~2900 tests covering all 6 JSON Schema drafts
- Test cases loaded from JSON-Schema-Test-Suite (git submodule)
- Tests include: spec compliance, output formats, thread safety, error handling, compiled validators, production schemas
- Remote schemas must be registered in `SchemaRepository` before use

## Versioning

Semantic Versioning: major for breaking changes, minor for new features, patch for fixes.
