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

- **Drafts:** 2020-12, 2019-09, and 4 are supported by the capability gate. Drafts 3, 6, and 7 are still rejected pre-emission and tracked as follow-up work.
- **External `$ref`:** generated validators can resolve external refs through a JS registry passed to `validate(data, registry)`. In practice that registry is required for schemas with external refs; missing registry or missing entries fail validation. The JS test-suite fixture preloads the official remote schemas plus the bundled Draft 2020-12 metaschemas; a handful of suite cases (notably cross-draft `$schema`-aware ref resolution and metaschema preloading of dynamic-ref behavior) remain skipped.
- **Annotation tracking:** `unevaluatedProperties` and `unevaluatedItems` are supported under Draft 2020-12 and Draft 2019-09 through generated evaluated-state tracking.
- **External-ref annotation propagation depends on the registry entry shape:** generated validators export `validateWithState(...)` and preserve `unevaluated*` annotations across external refs when the registry entry exposes that method. Plain function validators and `{ validate(...) }` objects still validate correctly but do not propagate evaluated-state annotations back to the caller.
- **Dynamic-scope refs:** `$dynamicRef` / `$dynamicAnchor` are supported under Draft 2020-12 via an immutable scope stack (`CompiledValidatorScope`). `$recursiveRef` / `$recursiveAnchor` (Draft 2019-09) are still rejected pre-emission.
- **Vocabulary-aware keyword emission:** the JS generator inspects the enclosing schema's metaschema (when preloaded via `ExternalSchemaDocuments`) and short-circuits validation-vocabulary keywords (`type`, `enum`, `const`, numeric/string/array/object length constraints, `pattern`, `minContains`/`maxContains`) to no-op emission when the metaschema does not include the validation vocabulary. This differs from the C# compiled target, which does not gate on `$vocabulary`. Format-assertion is handled separately via `FormatAssertionEnabled` / `$vocabulary: format-assertion`.
- **Annotation tracking clone cost:** schemas that combine `unevaluated*` with deeply nested or high-fanout `allOf`/`anyOf`/`oneOf` conditionals clone evaluated-state maps while validating. This is correct but can add overhead for large object/array instances.

### Numeric Precision

JavaScript numbers are IEEE-754 doubles. Integer detection and `multipleOf` comparisons use the same precision as the C# compiled path (`double`/`TryGetDouble`). There is no BigInt fast path — large integers beyond 2^53 lose precision on both sides, matching behavior rather than adding divergence.

### String Length Counting

`minLength`/`maxLength` count grapheme clusters via `Intl.Segmenter` when available (matches C# `StringInfo.LengthInTextElements`). On environments without `Intl.Segmenter` (non-ICU builds), the runtime falls back to code-point counting (`Array.from(str).length`), which handles surrogate pairs but not combining marks or ZWJ sequences.

### Regex Execution

Patterns emit JavaScript `new RegExp("...")` constructor expressions (ECMAScript flavor, matching the JSON Schema spec). Constructor form is used instead of the `/pattern/` literal form so schema-supplied text never participates in JS tokenisation — patterns starting with `*` or containing other tokenizer hazards can no longer break module parsing. Invalid ECMAScript regex grammar surfaces at `RegExp` construction time rather than as a module parse error. JavaScript has no regex timeout — pathological patterns that would trip C#'s `matchTimeoutMilliseconds` safeguard can hang in JS. Treat validator input as untrusted at your own discretion.

### Format Validation

Drafts 4 and 2019-09 continue to assert supported formats by default (matches the C# compiled path). Draft 2020-12 is annotation-only by default; set `FormatAssertionEnabled` on the JS generator or pass `--assert-format` to `jsv-codegen generate-js` to emit eager validation for supported Draft 2020-12 formats. When the enclosing metaschema declares the format-assertion vocabulary in `$vocabulary` (and is preloaded via `ExternalSchemaDocuments`), eager format validation is also enabled automatically. `idn-email`, `idn-hostname`, `iri`, and `iri-reference` each have dedicated validators: punycode-decoded Unicode hostname rules (including IDN contextual-rule checks — virama, Greek/Hebrew adjacency, katakana middle dot, Arabic/extended-Arabic-Indic digit mixing), and IRI authority parsing that recognises international character classes.

### Shared Runtime Module

Emitted validators import from a sibling `jsv-runtime.js`. The CLI writes both files by default; pass `--no-runtime` to suppress the runtime write if your build already provides one. The runtime ABI is versioned (`ABI VERSION: mvp-0`); bumping the version signals a breaking change to emitted validator expectations.

### TypeScript-First Migration Path

Issue #32 introduces a parallel TypeScript-first path without removing the direct JS generator. `jsv-codegen generate-ts` emits TypeScript source, and `jsv-codegen generate-js --pipeline typescript --ecmascript-target <target>` compiles that source with `tsc` to produce JavaScript for an explicit TypeScript compiler target. The direct JS path remains the default and should be kept available for performance comparison until the TypeScript path has proven parity.

Current migration constraints:

- The TS generator has its own orchestration and keyword emitter classes. The first implementation intentionally keeps emitted validator semantics aligned with the direct JS emitter so parity and performance comparisons remain meaningful.
- `jsv-runtime.ts` is currently derived from the stable `jsv-runtime.js` ABI and marked `// @ts-nocheck`; converting the runtime to fully typed, TS-authored source is still follow-up work.
- The package invokes an external `tsc` executable for JS-from-TS output. Install TypeScript separately or pass `--tsc <path>` to the CLI.
- Draft 2020-12 TS-derived JS is tested through the JSON-Schema-Test-Suite against the same enabled case set as the direct JS target. This is parity coverage for the current JS capability gate, not a claim that deferred JS-target capabilities are complete.
- Browser-query targeting and polyfill management remain out of scope. The exposed target is the `tsc` ECMAScript target, and consumers can apply their own downstream bundling or browser compatibility tooling.
