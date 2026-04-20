# Known Limitations

This document lists known limitations and edge cases. The library passes 100% of the JSON-Schema-Test-Suite tests across all 6 supported drafts — the items below are architectural trade-offs, platform constraints, and compiled validator gaps.

## Dynamic (DI-Based) Validator

### Number Hashing Precision

Schema hashing converts numbers through `double` (IEEE 754, ~15-17 significant digits). Two schemas differing only in numbers beyond double precision may hash identically. Practical impact is minimal — schemas rarely contain such numbers.

### No Automatic Remote Schema Fetching

Remote schemas (`$ref` to external URIs) must be pre-registered in `SchemaRepository`. The library does not fetch schemas over HTTP — automatic fetching would introduce security hazards (e.g., SSRF) beyond the scope of a validation library. Use `SchemaRepository.Register()` to load external schemas before validation.

## Compiled Validators

Compiled validators generate optimized code at runtime for faster repeated validation. They resolve references statically at compile time, which means certain dynamic features are not supported.

### Complex `$dynamicRef` Scenarios (Draft 2020-12)

Basic `$dynamicRef` / `$dynamicAnchor` resolution works. Complex scenarios involving external schemas or multiple dynamic paths through different `$ref` chains may not resolve correctly, because full dynamic scope resolution requires runtime stack inspection.

### Vocabulary-Based Validation (Drafts 2019-09, 2020-12)

Compiled validators cannot enable or disable keywords based on `$vocabulary` declarations in metaschemas. Vocabulary processing requires runtime evaluation of the metaschema.

### Cross-Draft Reference Semantics

Compiled validators cannot process `$ref` targets according to their declared `$schema`. When a schema references a subschema from a different draft, the compiled validator applies Draft 2020-12 semantics uniformly.

## .NET Platform Limitations

### Float Normalization

`System.Text.Json` normalizes `1.0` to `1`. The library cannot distinguish between integer and float representations of the same number. This affects the `zeroTerminatedFloats` optional test (Drafts 3 and 4). Not fixable without custom JSON parsing.

## JavaScript Code-Gen Target (`jsv-codegen generate-js`)

The JS target emits compiled validators for consumption by JavaScript/TypeScript projects (ESM modules, importable from Node or bundlers). MVP scope is deliberately narrower than the C# target; items below are either active MVP constraints or documented behavioral gaps.

### MVP Scope

- **Drafts:** 2020-12 and 4 only. Other drafts (3, 6, 7, 2019-09) are rejected pre-emission. Tracked as follow-up work.
- **Self-contained schemas with local `$ref` only.** External `$ref` is rejected pre-emission. The registry/fragment subschema machinery that exists for C# has not been ported.
- **Deferred features rejected:** `unevaluatedProperties`, `unevaluatedItems`, `$dynamicRef`, `$dynamicAnchor`, `$recursiveRef`, `$recursiveAnchor`. The capability gate surfaces a structured error naming the unsupported keyword.

### Numeric Precision

JavaScript numbers are IEEE-754 doubles. Integer detection and `multipleOf` comparisons use the same precision as the C# compiled path (`double`/`TryGetDouble`). There is no BigInt fast path — large integers beyond 2^53 lose precision on both sides, matching behavior rather than adding divergence.

### String Length Counting

`minLength`/`maxLength` count grapheme clusters via `Intl.Segmenter` when available (matches C# `StringInfo.LengthInTextElements`). On environments without `Intl.Segmenter` (non-ICU builds), the runtime falls back to code-point counting (`Array.from(str).length`), which handles surrogate pairs but not combining marks or ZWJ sequences.

### Regex Execution

Patterns emit native JavaScript `RegExp` literals (ECMAScript flavor, matching the JSON Schema spec). JavaScript has no regex timeout — pathological patterns that would trip C#'s `matchTimeoutMilliseconds` safeguard can hang in JS. Treat validator input as untrusted at your own discretion.

### Format Validation

Supported formats are eager-validated (same stance as the C# compiled path). This intentionally diverges from the 2020-12 suite's "annotation-only by default" expectation; suite cases asserting annotation-only behavior are excluded from our test runner. For `idn-email`, `idn-hostname`, `iri`, and `iri-reference`, the runtime aliases to the ASCII counterpart — IDN-specific validation is deferred.

### Shared Runtime Module

Emitted validators import from a sibling `jsv-runtime.js`. The CLI writes both files by default; pass `--no-runtime` to suppress the runtime write if your build already provides one. The runtime ABI is versioned (`ABI VERSION: mvp-0`); bumping the version signals a breaking change to emitted validator expectations.
