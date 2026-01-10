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
- **Annotation Support** - Per-keyword annotations for detailed output:
  - `properties` - Lists validated property names
  - `patternProperties` - Lists matched property names
  - `additionalProperties` - Lists additional property names
  - `prefixItems` - Largest validated index or `true`
  - `items` - `true` when items were validated
  - `contains` - List of matching indices
  - `unevaluatedItems` - List of validated indices
  - `unevaluatedProperties` - List of validated property names
  - `format` - Format string value

---

## Gaps Identified

### 1. ~~Output Format Compliance - MISSING~~ ✓ FIXED

**Severity:** ~~High~~ Resolved

**Solution:** Implemented spec-compliant output formats per JSON Schema 2020-12 Section 12:
- **Flag** - Boolean only, most efficient
- **Basic** - Flat list of all errors with instance/keyword locations
- **Detailed** - Hierarchical nested structure with annotations

**New Classes:**
- `Common/JsonPointer.cs` - RFC 6901 JSON Pointer implementation for location tracking
- `Validation/Output/OutputUnit.cs` - Spec-compliant output structure
- `Validation/Output/OutputFormat.cs` - Enum for Flag/Basic/Detailed formats

**Updated `ValidationResult` (now a record):**
```csharp
public record ValidationResult
{
    public bool IsValid { get; }
    public string InstanceLocation { get; }       // JSON Pointer to instance
    public string KeywordLocation { get; }        // JSON Pointer to schema keyword
    public string? AbsoluteKeywordLocation { get; init; }
    public string? Error { get; }
    public string? Keyword { get; init; }
    public IReadOnlyDictionary<string, object?>? Annotations { get; init; }
    public IReadOnlyList<ValidationResult>? Children { get; init; }

    public OutputUnit ToOutputUnit(OutputFormat format);
}
```

**New Extension Methods (`Common/SchemaValidatorExtensions.cs`):**
```csharp
validator.ValidateFlag(context);     // Flag output
validator.ValidateBasic(context);    // Basic output with flat errors
validator.ValidateDetailed(context); // Detailed hierarchical output
```

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

### 3. ~~Error Detail Structure - LIMITED~~ ✓ FIXED

**Severity:** ~~Medium~~ Resolved

**Solution:** All validators now track and return structured error details including:
- `instanceLocation` - JSON pointer to the failing instance ✓
- `keywordLocation` - JSON pointer to the schema keyword that failed ✓
- `absoluteKeywordLocation` - Absolute URI with JSON pointer ✓
- `error` - Human-readable error message ✓

**Files Changed:** All 60+ keyword validators in `Draft202012/Keywords/` were updated to use the new `Validate(IJsonValidationContext context, JsonPointer keywordLocation)` signature.

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
- 473 total tests (448 JSON-Schema-Test-Suite + 25 output format & annotation tests)
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

**Overall: ~95% Compliant**

| Area | Status |
|------|--------|
| Keyword Implementation | ✓ Complete |
| Vocabulary Support | ✓ Complete |
| Format Validators | ✓ Complete |
| Remote $ref Resolution | ✓ Complete |
| Dynamic References | ✓ Complete |
| Output Format | ✓ Flag/Basic/Detailed |
| Error Structure | ✓ Complete with JSON Pointers |
| Validator Ordering | ✓ Guaranteed via ExecutionOrder |
| Annotations | ✓ Full support for applicator keywords |

**Production Ready:** Yes
**Spec-Compliant Output:** Yes - Flag, Basic, and Detailed formats with annotation support

---

## Recommended Next Steps

1. ~~**High Priority:** Implement spec-compliant output formats~~ ✓ Done
2. ~~**High Priority:** Add validator execution ordering mechanism~~ ✓ Done
3. ~~**Medium Priority:** Enhance error structure with instance/schema paths~~ ✓ Done
4. ~~**Medium Priority:** Add annotation support for applicator keywords~~ ✓ Done
5. **Low Priority:** Add cross-draft compatibility support
