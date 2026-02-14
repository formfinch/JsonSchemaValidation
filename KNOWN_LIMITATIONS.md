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
