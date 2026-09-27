# Extended format catalog

An opt-in set of named string formats beyond the JSON Schema specification: bank, book and national identifiers that forms commonly ask for. This document is the reference for each format. A port of the library (another language, generated code) implements from this document and the shared test vectors in [`JsonSchemaValidationTests/TestData/Formats`](JsonSchemaValidationTests/TestData/Formats), which use the JSON-Schema-Test-Suite file shape.

## Enabling

```csharp
var options = new SchemaValidationOptions { EnableExtendedFormats = true };
options.Draft202012.FormatAssertionEnabled = true;

var result = JsonSchemaValidator.Validate("""{"format": "nl-phone"}""", "\"06-12345678\"", options);
```

- `EnableExtendedFormats` is off by default. When it is off, catalog names behave like any unknown format: an annotation, never a failure.
- Catalog formats assert only when format assertion is active for the schema's draft (the draft's `FormatAssertionEnabled` option, or the format-assertion vocabulary in 2019-09 and 2020-12). Otherwise they are annotations, as the specification requires.
- Built-in format names take precedence; catalog names do not overlap with them.
- Non-string instances are ignored, like every other format.
- The catalog is used through the `format` keyword. Its only public member, `ExtendedFormats.IsValid(format, element)`, exists for generated validators (see below).

Compiled C# validators support the catalog when generated with `jsv-codegen generate --extended-formats` (or `CSharpCodeGenerationOptions.ExtendedFormats`). Generating with the flag is the opt-in: like the built-in formats in compiled validators, catalog formats are then always asserted, and the generated code calls `FormFinch.JsonSchemaValidation.Formats.ExtendedFormats.IsValid`. Without the flag they are annotations.

The JavaScript and TypeScript generators support the catalog with `jsv-codegen generate-js --extended-formats` / `generate-ts --extended-formats` (or `ExtendedFormats` on their options). There, catalog formats follow the same rule as built-in formats in that target: Drafts 4 and 2019-09 assert them, and Draft 2020-12 asserts them only with format assertion enabled (`--assert-format`, or a metaschema whose `$vocabulary` declares format-assertion); the CLI warns when `--extended-formats` is used without `--assert-format`. Validators generated with the flag import the catalog functions, so they need a `jsv-runtime` from this version or later. The checks live in the generated `jsv-runtime.js` / `jsv-runtime.ts` (`isValidIso13616Iban`, `isValidNlPhone`, …) and mirror the C# checks; the same vectors run against every path.

Status: the runtime validator, compiled C#, and the JavaScript and TypeScript generators support the catalog.

## Input rules

Each format accepts the common ways people write a value. A check never changes the value; it only decides whether the value is acceptable. Normalizing for storage or display is the caller's concern.

Rules shared by all formats:

- Leading and trailing whitespace is allowed: space, no-break space (U+00A0), tab, CR, LF.
- A *space* inside a value is U+0020 or U+00A0 (common in pasted text).
- Separators listed for a format are ignored wherever they appear. Any other character that is not part of the format makes the value invalid.
- Letters are ASCII only. Where a format says *any case*, lower-case letters are read as upper case.

A passing check proves a value is well-formed. It does not prove the value exists (a VAT number can pass and still not be registered).

## Formats

| Name | What | Accepted input |
| --- | --- | --- |
| `iso-13616-iban` | International Bank Account Number | any case; spaces |
| `iso-9362-bic` | Business Identifier Code | any case; spaces |
| `iso-2108-isbn` | ISBN (13 digits) | spaces and hyphens; optional `ISBN` prefix |
| `nl-bsn` | Dutch citizen service number (BSN) | spaces, dots and hyphens |
| `nl-vat` | Dutch VAT identification number | any case; spaces and dots |
| `nl-kvk` | Dutch Chamber of Commerce number | spaces |
| `nl-postcode` | Dutch postcode | any case; at most one space |
| `nl-phone` | Dutch phone number | spaces, hyphens, dots, slashes, parentheses |
| `be-phone` | Belgian phone number | spaces, hyphens, dots, slashes, parentheses |

### `iso-13616-iban`

Source: ISO 13616; country lengths from the SWIFT IBAN Registry (89 countries; checked in September 2026 against the registry table as reproduced on Wikipedia, to be re-checked against the registry release itself).

1. Remove spaces; upper-case.
2. Two letters (country), two digits (check digits), then letters and digits.
3. The country must be in the registry and the length must equal the registry length for that country.
4. ISO 7064 mod 97-10: move the first four characters to the end, read letters as 10–35, and the remainder mod 97 must be 1.

Examples: `NL91 ABNA 0417 1643 00`, `nl91abna0417164300`, `BE68 5390 0754 7034`. Not accepted: dashes, unknown country, wrong length, wrong check digits.

### `iso-9362-bic`

Source: ISO 9362:2014 and later. Structure only; no directory lookup.

1. Remove spaces; upper-case.
2. 8 or 11 characters: 4 letters or digits (business party prefix), 2 letters (country code), 2 letters or digits (business party suffix), optionally 3 letters or digits (branch).

Editions before 2014 required the first four characters to be letters; the 2014 edition allows digits there, so this format accepts both.

Examples: `ABNANL2A`, `abna nl 2a`, `DEUTDEFF500`.

### `iso-2108-isbn`

Source: ISO 2108:2005 and later, which define the 13-digit ISBN. The 10-digit ISBN of earlier editions is not accepted.

1. Optional prefix, any case: `ISBN`, optionally followed by `-13`, optionally followed by `:`, then optional whitespace.
2. Remove spaces and hyphens.
3. 13 digits starting with `978` or `979`.
4. Weighted sum with weights 1, 3, 1, 3, … over all 13 digits is divisible by 10.

Examples: `978-0-306-40615-7`, `ISBN-13: 978-0-306-40615-7`, `isbn:9780306406157`. Not accepted: ISBN-10, an `ISBN-10` label, other prefixes.

### `nl-bsn`

Source: Dutch BSN rules (elfproef / 11-check).

1. Remove spaces, dots and hyphens.
2. 8 or 9 digits; an 8-digit value is read with a leading `0`.
3. Not all zeros.
4. `9*d1 + 8*d2 + 7*d3 + 6*d4 + 5*d5 + 4*d6 + 3*d7 + 2*d8 - 1*d9` is divisible by 11 (d1 is the first digit).

Examples: `111222333`, `111.222.333`, `12345672`.

### `nl-vat`

Source: Belastingdienst VAT identification number (btw-identificatienummer).

1. Remove spaces and dots; upper-case.
2. `NL`, 9 digits, `B`, 2 digits from `01` to `99` (`00` is never issued).
3. Valid when **either**:
   - the 9 digits pass the 11-check of `nl-bsn` (numbers issued before 2020), **or**
   - the whole identifier passes ISO 7064 mod 97-10 with letters read as 10–35 (the btw-id issued to sole proprietors since 2020).

Examples: `NL004495445B01` (11-check), `NL000099998B57` (mod 97), `nl 0044.95.445.b01`.

### `nl-kvk`

Source: Kamer van Koophandel. KvK numbers have no check digit; structure only.

1. Remove spaces.
2. 8 digits, not all zeros.

Examples: `12345678`, `1234 5678`.

### `nl-postcode`

Source: PostNL postcode system.

1. Four digits, the first not `0`.
2. Optionally one space.
3. Two letters, any case. `SA`, `SD` and `SS` are not issued and are rejected.

Examples: `1234AB`, `1234 ab`. Not accepted: `1234-AB`, two spaces, `1234 SS`.

### `nl-phone` and `be-phone`

Sources: Dutch numbering plan (ACM / Rijksinspectie Digitale Infrastructuur) and Belgian numbering plan (BIPT). Structure and length only; no check that a range is in service.

Reading the number (both formats):

1. Allowed characters: digits, a leading `+`, separators (space, hyphen, dot, slash), and parentheses. Parentheses must be balanced, not nested, and contain at least one digit.
2. The country calling code is `31` for `nl-phone` and `32` for `be-phone`. Accepted prefixes:
   - international: `+31 …` or `0031 …`. An optional trunk `0` may follow the country code, written as `(0)` or not: `+31 (0)6 …`, `+31 06 …`.
   - national: `0 …`.
3. The remaining digits are the significant number. It must not start with `0`. A number with another country's code is rejected.

Significant number:

- `nl-phone`: 9 digits.
- `be-phone`: 8 digits (landline and non-geographic), or 9 digits starting with `45`–`49` (mobile).

Examples, `nl-phone`: `06-12345678`, `+31 (0)6 12345678`, `0031 6 12345678`, `(020) 123 4567`.
Examples, `be-phone`: `0475 12 34 56`, `0475/12.34.56`, `+32 475 12 34 56`, `02 123 45 67`.

A national number can be valid in both countries (`0475 12 34 56` is a Belgian mobile and a Dutch landline in area 0475). To accept numbers from several countries, combine formats with `anyOf`:

```json
{ "anyOf": [{ "format": "nl-phone" }, { "format": "be-phone" }] }
```

## Adding a format

A format belongs in the catalog only when:

- **Its rule is stable.** The structure and check are fixed by a standard or statute and not expected to change. Rules that shift over time (for example which number ranges are in service) stay out.
- **It checks form, not existence.** The check decides whether a value is well-formed from the value alone. Anything that needs reference data (does this postcode, VAT number or account exist?) is out of scope.

Steps:

1. Add the check to `ExtendedFormatValidators` (internal) and its name to the catalog table.
2. Port the check to `jsv-runtime.ts` and `jsv-runtime.js` as an exported `isValid…` function, declare it in `TsRuntime.GetDeclarationSource`, and map the name in `JsFormatCodeGenerator` and `TsFormatCodeGenerator` (`MapExtendedFormatToImport`).
3. Add a vector file `TestData/Formats/<name>.json` with valid inputs, invalid inputs, and the documented input variants. The runtime, compiled C#, JS and TS tests all run it.
4. Document the format here: source, rule, accepted input, examples.

Names are `<authority>-<kind>`, where the authority is the body that defines the rule:

- a country, as its ISO 3166-1 alpha-2 code: `nl-bsn`, `nl-vat`, `be-phone`, `gb-vat`;
- a standards body and the number of the standard, for rules that are the same everywhere: `iso-13616-iban`, `iso-9362-bic`, `iso-2108-isbn`. The number identifies the standard; its section in this document states which edition's rule the format implements.

Country codes are two letters and standards bodies are longer, so the two can never collide. A group such as "any EU VAT number" is not a format; combine the national formats with `anyOf`.
