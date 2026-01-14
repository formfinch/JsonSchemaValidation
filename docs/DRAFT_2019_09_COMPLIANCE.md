# JSON Schema Draft 2019-09 Compliance

This document tracks the implementation status and compliance of JsonSchemaValidation against JSON Schema Draft 2019-09.

## Test Suite Results

**Overall Pass Rate: 98.2%** (427/435 tests from JSON-Schema-Test-Suite)

| Test Category | Passed | Total | Notes |
|--------------|--------|-------|-------|
| Core Validation | 385 | 393 | 8 failures in $recursiveRef edge cases |
| Format Assertion | 42 | 42 | All format validators pass |

## Implemented Keywords

### Core Keywords
- `$id` - Schema identification
- `$schema` - Draft version declaration
- `$ref` - Static reference (with sibling keywords support)
- `$anchor` - Named anchors
- `$recursiveRef` - Dynamic recursive references
- `$recursiveAnchor` - Mark schema for dynamic resolution
- `$vocabulary` - Vocabulary declaration
- `$comment` - Schema comments (ignored during validation)
- `$defs` - Schema definitions

### Validation Keywords
- `type` - Type validation (single and multiple types)
- `const` - Constant value validation
- `enum` - Enumeration validation
- `multipleOf` - Numeric multiple validation
- `maximum` / `minimum` - Numeric bounds
- `exclusiveMaximum` / `exclusiveMinimum` - Exclusive bounds
- `maxLength` / `minLength` - String length constraints
- `pattern` - Regex pattern matching
- `maxItems` / `minItems` - Array length constraints
- `uniqueItems` - Array uniqueness
- `maxContains` / `minContains` - Contains count constraints
- `maxProperties` / `minProperties` - Object property count
- `required` - Required properties
- `dependentRequired` - Conditional required properties

### Applicator Keywords
- `allOf` / `anyOf` / `oneOf` - Boolean logic combinators
- `not` - Negation
- `if` / `then` / `else` - Conditional validation
- `properties` - Object property schemas
- `patternProperties` - Regex-matched property schemas
- `additionalProperties` - Remaining property schema
- `propertyNames` - Property name validation
- `dependentSchemas` - Conditional schemas
- `items` - Array item validation (both array and schema forms)
- `additionalItems` - Additional array items schema
- `contains` - Array containment

### Unevaluated Keywords
- `unevaluatedItems` - Remaining array items
- `unevaluatedProperties` - Remaining object properties

### Format Keywords
All 19 format types are supported with assertion validation:
- `date-time`, `date`, `time`, `duration`
- `email`, `idn-email`
- `hostname`, `idn-hostname`
- `ipv4`, `ipv6`
- `uri`, `uri-reference`, `iri`, `iri-reference`, `uri-template`
- `uuid`
- `json-pointer`, `relative-json-pointer`
- `regex`

### Content Keywords
- `contentEncoding` - Base64 encoding validation
- `contentMediaType` - Media type annotation
- `contentSchema` - Content schema validation

## Known Limitations

### $recursiveRef Edge Cases (8 failing tests)

The following advanced $recursiveRef scenarios have edge cases that don't pass the test suite:

1. **$recursiveRef without using nesting** - Complex scenarios where $recursiveRef appears in nested schemas without nesting through a $ref chain
2. **$recursiveRef with $recursiveAnchor: false** - Interaction between false recursive anchors
3. **Multiple dynamic paths to $recursiveRef** - Complex if/then/else with multiple $recursiveRef paths
4. **Dynamic $recursiveRef destination** - Scenarios where the target is not statically determinable
5. **$ref with $recursiveAnchor** - Interaction between $ref and $recursiveAnchor
6. **unevaluatedItems/unevaluatedProperties with $recursiveRef** - Complex annotation tracking through recursive references

These represent advanced meta-programming scenarios that are rarely used in typical schemas. Basic $recursiveRef usage (like validating recursive data structures) works correctly.

### Cross-Draft Compatibility

Cross-draft validation (mixing schemas from different draft versions) is not supported and is considered out of scope.

## Architecture Notes

### Full Draft Separation
Draft 2019-09 is implemented as a completely separate namespace (`JsonSchemaValidation.Draft201909`) from Draft 2020-12. This ensures:
- No shared validator code between drafts
- Clear separation of draft-specific behavior
- Ability to enable/disable drafts independently

### Multi-Draft Coexistence
Both Draft 2019-09 and Draft 2020-12 can be enabled simultaneously:
```csharp
services.AddJsonSchemaValidation(opt =>
{
    opt.EnableDraft202012 = true;
    opt.EnableDraft201909 = true;
});
```

The validator automatically selects the correct draft based on the `$schema` keyword.

### Key Differences from Draft 2020-12

| Feature | Draft 2019-09 | Draft 2020-12 |
|---------|--------------|---------------|
| Recursive references | `$recursiveRef` / `$recursiveAnchor` | `$dynamicRef` / `$dynamicAnchor` |
| Array tuple validation | `items` (array) + `additionalItems` | `prefixItems` + `items` |
| Format vocabulary | `format` (annotation) | `format-annotation` / `format-assertion` |

## Vocabulary Support

The implementation fully supports Draft 2019-09 vocabularies:

- `https://json-schema.org/draft/2019-09/vocab/core`
- `https://json-schema.org/draft/2019-09/vocab/applicator`
- `https://json-schema.org/draft/2019-09/vocab/validation`
- `https://json-schema.org/draft/2019-09/vocab/meta-data`
- `https://json-schema.org/draft/2019-09/vocab/format`
- `https://json-schema.org/draft/2019-09/vocab/content`

Custom meta-schemas with selective vocabulary declarations are supported.
