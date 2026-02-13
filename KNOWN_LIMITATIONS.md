# Known Limitations

This document lists known limitations and edge cases. The library passes 100% of the JSON-Schema-Test-Suite tests across all 6 supported drafts — the items below are architectural trade-offs, platform constraints, and compiled validator gaps.

## Dynamic (DI-Based) Validator

### Static API Schema Caching

The static `JsonSchemaValidator` API caches schemas by content hash. The hash excludes `$id` for performance, so two schemas differing only by `$id` share a cached validator. This means:

- Internal `$ref: "#"` resolves to the first schema's base URI
- Output locations show the first schema's URI
- The second schema's `$id` is never registered

The boolean valid/invalid result is unaffected. Use the DI-based API if `$id` correctness matters.

### Number Hashing Precision

Schema hashing converts numbers through `double` (IEEE 754, ~15–17 significant digits). Two schemas differing only in numbers beyond double precision may hash identically. Practical impact is minimal — schemas rarely contain such numbers.

### No Automatic Remote Schema Fetching

Remote schemas (`$ref` to external URIs) must be pre-registered in `SchemaRepository`. The library does not fetch schemas over HTTP. Use `SchemaRepository.Register()` to load them before validation.

### Annotation-Only Keywords Not Yet Emitted

The following keywords are correctly ignored during validation (per spec) but do not yet produce annotations in `ValidateDetailed()` output:

| Keyword | Drafts |
|---------|--------|
| `title` | 4, 6, 7, 2019-09, 2020-12 |
| `description` | 4, 6, 7, 2019-09, 2020-12 |
| `default` | 3, 4, 6, 7, 2019-09, 2020-12 |
| `deprecated` | 2019-09, 2020-12 |
| `readOnly` | 7, 2019-09, 2020-12 |
| `writeOnly` | 7, 2019-09, 2020-12 |
| `examples` | 6, 7, 2019-09, 2020-12 |
| `$comment` | 7, 2019-09, 2020-12 |

These do not affect validation results — only annotation output.

## Compiled Validators

Compiled validators generate optimized code at runtime for faster repeated validation. They resolve references statically at compile time, which means certain dynamic features are not supported.

### Dynamic Reference Resolution

Compiled validators cannot perform runtime dynamic scope resolution. The following keywords require stack inspection at validation time to find matching anchors in the call chain:

- **`$dynamicRef` / `$dynamicAnchor`** (Draft 2020-12) — basic cases work; complex scenarios with external schemas or multiple dynamic paths may not resolve correctly
- **`$recursiveRef` / `$recursiveAnchor`** (Draft 2019-09) — requires runtime recursive scope resolution

### Unevaluated Keywords

`unevaluatedItems` and `unevaluatedProperties` require annotation tracking across applicators to determine which items/properties have been evaluated. This is not fully supported in compiled validators when combined with applicator keywords like `allOf`, `anyOf`, `oneOf`, or `if`/`then`/`else`.

### Infinite Loop Detection

Compiled validators resolve `$ref` statically without tracking visited nodes. Recursive schemas that create infinite reference loops will cause a stack overflow. The dynamic validator handles this with a recursion depth limit.

### Vocabulary-Based Validation

Compiled validators cannot enable or disable keywords based on `$vocabulary` declarations in metaschemas. Vocabulary processing requires runtime evaluation of the metaschema.

### Content Validation

`contentMediaType` and `contentEncoding` validation (e.g., base64 decoding, JSON parsing of string content) is not implemented in compiled validators.

### Cross-Draft Reference Semantics

Compiled validators cannot process `$ref` targets according to their declared `$schema`. When a schema references a subschema from a different draft, the compiled validator applies Draft 2020-12 semantics uniformly.

### Draft 7 and Earlier

| Limitation | Details |
|-----------|---------|
| `$ref` overrides siblings | In Draft 7 and earlier, `$ref` causes all sibling keywords to be ignored. Compiled validators apply 2020-12 semantics where siblings are evaluated. |
| `$id: "#fragment"` anchors | Older drafts use `$id` with a fragment for location-independent identifiers. Compiled validators use the `$anchor` keyword from 2019-09+. |
| `id` vs `$id` resolution | Drafts 4 and earlier use `id` (without `$`) with different resolution rules. Not supported. |
| Base URI changes | When a subschema changes the base URI via `$id`/`id`, reference resolution may require full document context that is unavailable at compile time. |
| Anchor resolution | Some anchor resolution scenarios require access to the full document context. |
| Remote refs with internal refs | Subschemas extracted from remote schemas that contain `$ref` to sibling definitions cannot be compiled standalone. |

### Draft-Specific Keywords Not Supported

| Keyword | Drafts | Notes |
|---------|--------|-------|
| `additionalItems` | 3, 4, 6, 7, 2019-09 | Tuple validation with array-form `items` |
| `items` (array form) | 3, 4, 6, 7 | Array of schemas for positional validation |
| `dependencies` | 3, 4, 6, 7 | Schema and property-array forms |
| `divisibleBy` | 3 | Precursor to `multipleOf` |
| `extends` | 3 | Precursor to `allOf` |
| `disallow` | 3 | Inverse of `type` |
| `required` (boolean) | 3 | Per-property boolean instead of array |
| `type` (schema arrays) | 3 | Union types via array of schemas |
| Draft 3 format names | 3 | `color`, `host-name`, `ip-address` |
| Metaschema validation | Varies | Validating against the metaschema itself |

## .NET Platform Limitations

### Float Normalization

`System.Text.Json` normalizes `1.0` to `1`. The library cannot distinguish between integer and float representations of the same number. This affects the `zeroTerminatedFloats` optional test (Drafts 3 and 4). Not fixable without custom JSON parsing.
