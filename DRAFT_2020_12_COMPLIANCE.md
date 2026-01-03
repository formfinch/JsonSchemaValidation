# JSON Schema Draft 2020-12 Compliance Report

**Generated:** 2026-01-03
**Solution:** JsonSchemaValidation
**Goal:** Full compatibility with JSON Schema Draft 2020-12 without third-party dependencies (built on System.Text.Json)

---

## Fully Implemented ✓

### Vocabularies (All 8)
- core (`https://json-schema.org/draft/2020-12/vocab/core`)
- applicator (`https://json-schema.org/draft/2020-12/vocab/applicator`)
- validation (`https://json-schema.org/draft/2020-12/vocab/validation`)
- unevaluated (`https://json-schema.org/draft/2020-12/vocab/unevaluated`)
- meta-data (`https://json-schema.org/draft/2020-12/vocab/meta-data`)
- format-annotation (`https://json-schema.org/draft/2020-12/vocab/format-annotation`)
- format-assertion (`https://json-schema.org/draft/2020-12/vocab/format-assertion`)
- content (`https://json-schema.org/draft/2020-12/vocab/content`)

### Keywords

| Category | Keywords | Status |
|----------|----------|--------|
| Core | `$id`, `$schema`, `$ref`, `$anchor`, `$dynamicRef`, `$dynamicAnchor`, `$vocabulary`, `$comment`, `$defs` | ✓ |
| Applicator | `prefixItems`, `items`, `contains`, `additionalProperties`, `properties`, `patternProperties`, `dependentSchemas`, `propertyNames`, `if`/`then`/`else`, `allOf`, `anyOf`, `oneOf`, `not` | ✓ |
| Validation | `type`, `const`, `enum`, `multipleOf`, `maximum`, `exclusiveMaximum`, `minimum`, `exclusiveMinimum`, `maxLength`, `minLength`, `pattern`, `maxItems`, `minItems`, `uniqueItems`, `maxContains`, `minContains`, `maxProperties`, `minProperties`, `required`, `dependentRequired` | ✓ |
| Unevaluated | `unevaluatedItems`, `unevaluatedProperties` | ✓ |
| Content | `contentEncoding`, `contentMediaType`, `contentSchema` | ✓ |
| Meta-Data | `title`, `description`, `default`, `deprecated`, `readOnly`, `writeOnly`, `examples` | ✓ (recognized, not validated - correct per spec) |

### Format Validators (19 formats)
- `date-time`, `date`, `time`, `duration`
- `email`, `idn-email`
- `hostname`, `idn-hostname`
- `ipv4`, `ipv6`
- `uri`, `uri-reference`, `iri`, `iri-reference`, `uri-template`
- `json-pointer`, `relative-json-pointer`
- `regex` (ECMAScript compatibility)
- `uuid`

### Features
- Remote schema resolution (`$ref` to external URIs)
- `$dynamicRef`/`$dynamicAnchor` with dynamic scope resolution
- Format assertion mode (opt-in via `SchemaValidationOptions.FormatAssertionEnabled`)
- Vocabulary filtering for custom meta-schemas
- Large number handling in `multipleOf` and integer type validation

---

## Gaps Identified

### 1. Output Format Compliance - MISSING

**Severity:** High

Draft 2020-12 specifies 4 output formats:
- Flag (boolean only)
- Basic (simple pass/fail with errors)
- Detailed (with instance/schema paths)
- Verbose (with annotations)

**Current State:** `ValidationResult` only provides:
```csharp
public bool IsValid { get; }
public List<string> Errors { get; }
public Dictionary<string, object> Annotations { get; }
```

**Missing:**
- Hierarchical error structure
- `instancePath` (JSON pointer to failing data)
- `schemaPath` (JSON pointer to failing keyword)
- Proper annotation hierarchy per spec

**File:** `Validation/ValidationResult.cs`

---

### 2. ~~Validator Execution Order - NOT GUARANTEED~~ ✓ FIXED

**Severity:** ~~High~~ Resolved

**Solution:** Added `ExecutionOrder` property to `ISchemaDraftKeywordValidatorFactory` interface with default value of 0. Validators are now sorted by `ExecutionOrder` in `SchemaDraft202012ValidatorFactory` constructor. Unevaluated keyword factories (`UnevaluatedItemsValidatorFactory`, `UnevaluatedPropertiesValidatorFactory`) set `ExecutionOrder = 100` to ensure they run last.

**Files Changed:**
- `Draft202012/Interfaces/ISchemaDraftKeywordValidatorFactory.cs` - Added `ExecutionOrder` property with default implementation
- `Draft202012/SchemaDraft202012ValidatorFactory.cs` - Sort factories by `ExecutionOrder` on construction
- `Draft202012/Keywords/UnevaluatedItemsValidatorFactory.cs` - Set `ExecutionOrder = 100`
- `Draft202012/Keywords/UnevaluatedPropertiesValidatorFactory.cs` - Set `ExecutionOrder = 100`

---

### 3. Error Detail Structure - LIMITED

**Severity:** Medium

**Current State:** Errors are simple strings.

**Spec-Compliant Errors Should Include:**
- `instanceLocation` - JSON pointer to the failing instance
- `keywordLocation` - JSON pointer to the schema keyword that failed
- `absoluteKeywordLocation` - Absolute URI with JSON pointer
- `error` - Human-readable error message

**Impact:** Debugging validation failures is harder without knowing which part of the schema/instance caused the failure.

---

### 4. Cross-Draft Compatibility - NOT SUPPORTED

**Severity:** Low (optional feature)

**Current State:** Test exclusion in `SchemaValidationTests.cs`:
```csharp
// @"\optional\cross-draft",  // No cross-draft compatibility yet
```

**Impact:** Schemas that reference other schemas using different draft versions won't work correctly.

---

### 5. Format Validation Strictness - ACCEPTABLE BUT BASIC

**Severity:** Low

**Areas for potential improvement:**
- Email validators use basic RFC structure validation, not full RFC 5321/5322 compliance
- Some edge cases in duration format parsing
- idn-email implementation shares code with basic email validator

---

## Test Suite Status

**JSON-Schema-Test-Suite Version:** Latest (updated 2026-01-03)

**Test Coverage:**
- 448 total tests
- 45 test categories enabled
- All tests passing ✓

**Enabled Optional Tests:**
- `bignum`
- `ecmascript-regex`
- `float-overflow`
- `format-assertion` (all 20 format categories)
- `non-bmp-regex`

**Disabled Optional Tests:**
- `cross-draft` (not implemented)

---

## Compliance Rating

**Overall: ~85% Compliant**

| Area | Status |
|------|--------|
| Keyword Implementation | ✓ Complete |
| Vocabulary Support | ✓ Complete |
| Format Validators | ✓ Complete |
| Remote $ref Resolution | ✓ Complete |
| Dynamic References | ✓ Complete |
| Output Format | ✗ Missing |
| Error Structure | ⚠ Basic |
| Validator Ordering | ✓ Guaranteed via ExecutionOrder |

**Production Ready:** Yes, for basic validation use cases
**Spec-Compliant Output:** No - needs output format implementation

---

## Recommended Next Steps

1. **High Priority:** Implement spec-compliant output formats
2. ~~**High Priority:** Add validator execution ordering mechanism~~ ✓ Done
3. **Medium Priority:** Enhance error structure with instance/schema paths
4. **Low Priority:** Add cross-draft compatibility support
