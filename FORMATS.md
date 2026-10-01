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

Two formats deviate, as documented in their sections: `de-plate` also accepts the letters Ä, Ö and Ü (German district codes contain them), and a separator inside its letters marks the end of the district; in `de-leitweg` the hyphens are part of the format and spaces are not allowed.

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
| `be-postcode` | Belgian postcode | none |
| `nl-plate` | Dutch licence plate (kenteken) | any case; hyphens and spaces |
| `itu-e164-phone` | International phone number (E.164) | `+` or `00` required; spaces, hyphens, dots, slashes, parentheses |
| `gb-phone` | UK phone number | spaces, hyphens, dots, slashes, parentheses |
| `gb-postcode` | UK postcode | any case; at most one space |
| `gb-nhs` | NHS number | spaces and hyphens |
| `gb-vat` | UK VAT registration number | any case; optional `GB`/`XI`; spaces, dots and hyphens |
| `gb-crn` | Companies House company number | any case; spaces |
| `gb-sort-code` | UK bank sort code | spaces and hyphens |
| `gb-account-number` | UK bank account number | spaces and hyphens |
| `gb-nino` | National Insurance number | any case; spaces |
| `gb-upn` | Unique Pupil Number | any case; spaces |
| `gb-plate` | UK vehicle registration mark | any case; spaces |
| `de-phone` | German phone number | spaces, hyphens, dots, slashes, parentheses |
| `de-postcode` | German postcode (PLZ) | none |
| `de-vat` | German VAT identification number (USt-IdNr) | any case; spaces |
| `de-idnr` | German tax identification number (Steuer-IdNr) | spaces, dots, hyphens and slashes |
| `de-stnr` | German tax number (Steuernummer) | spaces, hyphens and slashes |
| `de-trade-register` | German commercial register number | any case; spaces |
| `de-leitweg` | Leitweg-ID (XRechnung) | any case; hyphens are part of the format |
| `de-rvnr` | German pension insurance number | any case; spaces |
| `de-kvnr` | German health insurance number | any case; spaces |
| `de-id-card` | German identity card number | any case; spaces |
| `de-passport` | German passport number | any case; spaces |
| `de-wkn` | German securities identification number (WKN) | any case; spaces |
| `de-plate` | German vehicle registration plate | any case, including Ä, Ö, Ü; hyphens and spaces |
| `at-phone` | Austrian phone number | spaces, hyphens, dots, slashes, parentheses |
| `at-postcode` | Austrian postcode | none |
| `at-vat` | Austrian VAT identification number (UID) | any case; spaces |
| `at-svnr` | Austrian social security number | spaces |
| `at-fn` | Austrian company register number (Firmenbuchnummer) | any case; optional `FN`; spaces |
| `at-tin` | Austrian tax number (Abgabenkontonummer) | spaces, hyphens and slashes |
| `at-plate` | Austrian licence plate | any case; hyphens and spaces |
| `at-passport` | Austrian passport number | any case; spaces |
| `ch-phone` | Swiss phone number | spaces, hyphens, dots, slashes, parentheses |
| `ch-postcode` | Swiss postcode | optional `CH-` |
| `ch-uid` | Swiss enterprise identification number (UID) | any case; spaces, dots and hyphens |
| `ch-vat` | Swiss VAT number | any case; spaces, dots and hyphens |
| `ch-ahv` | Swiss social security number (AHV) | spaces and dots |
| `ch-qr-reference` | Swiss QR-bill reference | spaces |
| `ch-qr-iban` | Swiss QR-IBAN | any case; spaces |
| `ch-plate` | Swiss licence plate | any case; hyphens, spaces and dots |
| `ch-passport` | Swiss passport or identity card number | any case; spaces |
| `li-phone` | Liechtenstein phone number | spaces, hyphens, dots, slashes, parentheses |
| `li-postcode` | Liechtenstein postcode | none |
| `li-peid` | Liechtenstein personal identification number | spaces and dots |
| `li-plate` | Liechtenstein licence plate | any case; hyphens and spaces |
| `lu-phone` | Luxembourg phone number | spaces, hyphens, dots, slashes, parentheses |
| `lu-postcode` | Luxembourg postcode | optional `L-` |
| `lu-vat` | Luxembourg VAT number | any case; spaces, dots and hyphens |
| `lu-matricule` | Luxembourg national identification number | spaces, dots and hyphens |
| `lu-rcs` | Luxembourg trade register number | any case; spaces |
| `lu-plate` | Luxembourg licence plate | any case; hyphens and spaces |

### `iso-13616-iban`

Source: ISO 13616; country lengths from the SWIFT IBAN Registry, release 101 (89 countries). Checked in September 2026 against `iban-registry-v101.txt` as distributed in python-stdnum's `iban.dat` (lengths computed from each country's BBAN structure): all 89 countries and lengths match. When SWIFT publishes a new release, update the table in the C#, JS and TS implementations together.

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

### Phone numbers of other countries

`gb-phone`, `de-phone`, `at-phone`, `ch-phone`, `li-phone` and `lu-phone` read the number as described for `nl-phone` and `be-phone`, with their own country calling code, and then check the significant number. Liechtenstein and Luxembourg have no trunk prefix: a national number is written without a leading `0`, a leading `0` is never accepted, and no `0` or `(0)` may follow the country code. Structure and length only; no check that a range is in service.

| Name | Code | Significant number |
| --- | --- | --- |
| `gb-phone` | 44 | 10 digits starting with 1, 2, 3, 5, 7, 8 or 9; or 9 digits in the mixed 01 areas (starting with `1`, second and third digit not `1`) or legacy `800` |
| `de-phone` | 49 | mobile `15…`: 11 digits; mobile `160`, `162`, `163`, `17…`: 10 or 11 digits; otherwise 6–13 digits |
| `at-phone` | 43 | 5–13 digits |
| `ch-phone` | 41 | 9 digits, the first 2–9 |
| `li-phone` | 423, no trunk prefix | 7 digits starting with 2, 3, 4, 7, 8 or 9; or 9 digits starting with 5 or 6 |
| `lu-phone` | 352, no trunk prefix | first digit 2–9; mobile (`6…`) 9 digits, otherwise 4–11 digits |

Sources: Ofcom National Telephone Numbering Plan (2025), Bundesnetzagentur numbering plans, RTR KEM-V 2009, BAKOM numbering plan (edition 8, 2024), Liechtensteinischer Nummerierungsplan (LGBl. 2007 Nr. 69), ILR Plan national de numérotation. Lengths cross-checked against libphonenumber metadata (September 2026).

Examples: `+44 (0)20 7946 0123`, `07700 900123`, `030 12345678`, `+49 1512 3456789`, `0664/123 45 67`, `+41 78 123 45 67`, `+423 234 56 78`, `234 56 78`, `+352 621 123 456`, `26 12 34 56`.
Not accepted: `0400 900123` (unused UK range), `0151 2345678` (German 15x mobile with 10 digits), `+423 (0)234 56 78`, `0621 123 456` (Luxembourg has no trunk `0`).

### `itu-e164-phone`

Source: ITU-T Recommendation E.164 (11/2010), clause 6: at most 15 digits; a country code of 1–3 digits that does not start with 0. Structure only: the country code is not looked up, because the list of assigned codes changes.

1. The value starts with `+` or with `00` (no separator between the zeros).
2. After the prefix: digits, separators (space, hyphen, dot, slash) and balanced, non-nested parentheses that contain a digit.
3. A first parenthesised group written exactly `(0)` after at least one digit is a trunk prefix and is not counted.
4. 7–15 digits remain, and the first is not `0`.

Examples: `+44 20 7946 0123`, `0044 20 7946 0123`, `+44 (0)20 7946 0123`, `+1 (555) 010-0123`, `+683 4002`.
Not accepted: a national number without prefix, `+0 123 4567`, 16 digits, `+(0)44 …`.

### Postcodes

| Name | Rule | Prefix |
| --- | --- | --- |
| `gb-postcode` | Outward code `A9`, `A99`, `AA9`, `AA99`, `A9A` or `AA9A`, inward code `9AA`, or `GIR 0AA`. Position 1 is not Q, V or X; position 2 (as a letter) is not I, J or Z; the letter in `A9A` is one of `ABCDEFGHJKPSTUW`, in `AA9A` one of `ABEHMNPRVWXY`; the inward letters are not C, I, K, M, O or V. Any case; at most one space, before the inward code. | none |
| `be-postcode` | 4 digits, the first not 0 | none |
| `de-postcode` | 5 digits, not starting with `00` | none |
| `at-postcode` | 4 digits, the first not 0 | none |
| `ch-postcode` | 4 digits, the first not 0 | optional `CH-`, any case |
| `li-postcode` | 9485–9498 (all codes in the range are assigned) | none |
| `lu-postcode` | 4 digits, the first not 0 | optional `L-`, any case |

Sources: Royal Mail PAF and the Government Data Standards Catalogue postcode pattern, bpost, Deutsche Post, Österreichische Post, Swiss Post, POST Luxembourg. Structure only: no list of issued codes, except for the small and complete Liechtenstein range.

Examples: `SW1A 1AA`, `sw1a1aa`, `GIR 0AA`, `1000`, `10115`, `1010`, `CH-8000`, `9490`, `L-1009`.
Not accepted: `SW1A-1AA`, `Q1 1AA`, `M1 1AC`, `0999`, `00123`, `D-10115`, `A-1010`, `CH 8000`, `9499`, `L1009`.

### `gb-nhs`

Source: NHS Data Dictionary, NHS Number (modulus 11).

1. Remove spaces and hyphens; 10 digits, not all zeros.
2. Multiply digits 1–10 by 10, 9, …, 1; the sum mod 11 is 0. (Equivalently: the check value is 11 − (weighted sum of digits 1–9 with weights 10–2) mod 11, 11 means 0, and 10 is never issued.)

Examples: `999 000 0018`, `999-000-0018`, `999 999 9999`. Not accepted: `999 000 0019`, `999 000 0000` (check value 10), `999.000.0018`.

### `gb-vat`

Sources: HMRC VAT registration numbers; the 9755 series (from November 2009); python-stdnum `gb/vat`.

1. Remove spaces, dots and hyphens; upper-case. An optional `GB` or `XI` prefix is removed.
2. `GD` + 000–499 (government departments) or `HA` + 500–999 (health authorities) is valid. These forms also exist with the `XI` prefix (the VIES format list for Northern Ireland includes `XIGD` and `XIHA`).
3. Otherwise 9 or 12 digits (a branch trader adds 3 digits, which are not checked). The first 7 digits are not all zeros.
4. Weights 8, 7, 6, 5, 4, 3, 2, 10, 1 on the first 9 digits; the sum mod 97 is 0, or 42 when the first digit is not 0 (9755 series: 55 is added before mod 97).

Examples: `GB 123 4567 82`, `GB 123 4567 27` (9755), `XI 123 4567 82`, `GB 123 4567 82 001`, `GBGD001`, `GBHA500`. Not accepted: `GB 123 4567 83`, `GB 001 2345 89`, `GBGD500`, `GB/123456782`.

### `gb-crn`

Sources: Companies House, URI Customer Guide (2012), list of company number prefixes; Companies House `CompanyPrefixes.pm` (September 2026), which adds BR, CE, CS, OE, PC and SG.

1. Remove spaces; upper-case; 8 characters.
2. 8 digits, not all zeros (England and Wales, written with leading zeros), or
3. a prefix from the list and 6 digits, not all zeros. Prefixes: AC, BR, CE, CS, EN, ES, FC, GE, GN, GS, IC, IP, LP, NA, NC, NF, NI, NL, NO, NP, NR, NV, NZ, OC, OE, PC, R0, RC, SA, SC, SE, SF, SG, SI, SL, SO, SP, SR, SZ, ZC.

The prefix list is maintained like the IBAN registry table: when Companies House adds a prefix, update C#, JS and TS together.

Examples: `01234567`, `SC123456`, `sc 123456`, `OE123456`, `R0123456`. Not accepted: `1234567`, `XX123456`, `SC000000`, `SC-123456`.

### `gb-sort-code` and `gb-account-number`

Source: Pay.UK / Vocalink account details. Structure only: modulus checking needs Vocalink's weight tables, which change several times a year.

- `gb-sort-code`: remove spaces and hyphens; 6 digits, not all zeros.
- `gb-account-number`: remove spaces and hyphens; 8 digits, not all zeros. Older 6, 7, 9 and 10-digit numbers are converted to 8 digits by the bank; they are not accepted.

Examples: `12-34-56`, `12 34 56`, `12345678`, `0123-4567`. Not accepted: `12.34.56`, `00-00-00`, `1234567`.

### `gb-nino`

Source: HMRC National Insurance Manual NIM39110.

1. Remove spaces; upper-case.
2. First letter not D, F, I, Q, U or V; second letter not D, F, I, O, Q, U or V; the pair is not BG, GB, KN, NK, NT, TN or ZZ.
3. 6 digits, then an optional suffix A–D.

Structure only. The GOV.UK placeholder `QQ 12 34 56 C` is deliberately invalid.

Examples: `AB 12 34 56 C`, `ab123456c`, `AB123456`. Not accepted: `QQ 12 34 56 C`, `TN 12 34 56 A`, `AB 12 34 56 E`, `AB-12-34-56-C`.

### `gb-upn`

Source: DfE, Unique Pupil Numbers (UPNs), guide 1.2 (2019), Annex A.

1. Remove spaces; upper-case; 13 characters: a check letter, 11 digits, then a digit (permanent UPN) or a letter (temporary UPN).
2. Letters come from `ABCDEFGHJKLMNPQRTUVWXYZ` (no I, O or S); a letter's value is its position in this alphabet, from 0.
3. The sum over characters 2–13 of position × value, mod 23, indexes the check letter, which must equal character 1.

The local authority code is not checked (reference data).

Examples: `H801200001001`, `X00180001701A`. Not accepted: `A801200001001`, `I801200001001`, `H801-2000-01-001`.

### Vehicle registration plates

Structure only: district and series lists are reference data. Plates identify vehicles; formats are shapes that the registration authority issues today, plus older shapes still on the road.

| Name | Accepted shapes | Separators |
| --- | --- | --- |
| `nl-plate` | 6 letters and digits in one of RDW sidecodes 1–14 (`XX-99-99`, `99-99-XX`, `99-XX-99`, `XX-99-XX`, `XX-XX-99`, `99-XX-XX`, `99-XXX-9`, `9-XXX-99`, `XX-999-X`, `X-999-XX`, `XXX-99-X`, `X-99-XXX`, `9-XX-999`, `999-XX-9`) | hyphens, spaces |
| `gb-plate` | current `AB12 CDE` (memory tag without I, Q, Z; random letters without I, Q); prefix `A123 BCD`; suffix `ABC 123D`; dateless `ABC 1234` or `1234 ABC`; numbers without leading 0; at most 7 characters | spaces |
| `de-plate` | district 1–3 letters (Ä, Ö and Ü allowed), 1–2 letters (ASCII), number 1–9999, optional `E` or `H`; at most 8 letters and digits, 7 with `E` or `H` | hyphens, spaces; a separator inside the letters marks the end of the district |
| `at-plate` | 1–2 letters, number 1–99999, 1–3 letters; or 1–7 letters and number 1–99999 (Wunschkennzeichen); no Q; at most 8 characters | hyphens, spaces |
| `ch-plate` | canton code (the 26 ISO 3166-2:CH codes) and number 1–999999 | hyphens, spaces, dots |
| `li-plate` | `FL` and number 1–99999 | hyphens, spaces |
| `lu-plate` | 2 letters (no I or O) and 4 or 2 digits, or 4–5 digits | hyphens, spaces |

Sources: RDW sidecodes; DVLA INF104; FZV 2023 (§ 9–11, Anlage 1 and 4); KDV 1967 and oesterreich.gv.at; VZV; Amt für Strassenverkehr Liechtenstein; Règlement grand-ducal du 17 juin 2003. Special plates (diplomatic, military, trade, temporary) are not accepted. `at-plate` is a loose check: without a district list a letter block cannot be split into district and Wunschkennzeichen.

Examples: `12-ABC-3`, `AB12 CDE`, `B-AB 123`, `KÜN-AB 12`, `B-AB 123 E`, `W 12345 A`, `ZH 123456`, `FL 12345`, `AB 1234`.
Not accepted: `ABC-123`, `IB12 CDE`, `B-ABC 123`, `B-ÄB 12`, `W 12 QA`, `XX 123`, `FL 01234`, `AI 1234`.

### `de-vat`

Sources: BZSt, structure of the USt-IdNr; ISO/IEC 7064 MOD 11,10; python-stdnum `de/vat`.

1. Remove spaces; upper-case; `DE` and 9 digits, the first not 0.
2. ISO 7064 MOD 11,10 over the 9 digits: start with p = 10; for each of the first 8 digits, s = (digit + p) mod 10, with 0 read as 10, and p = 2s mod 11; the check digit is 11 − p, with 10 read as 0.

Examples: `DE136695976`, `DE 136 695 976`. Not accepted: `136695976`, `DE136695978`, `DE036695976`.

### `de-idnr`

Source: Bayerisches Landesamt für Steuern, Prüfung der Steuer- und Steueridentifikationsnummer (ELSTER, September 2026), § 2.2.

1. Remove spaces, dots, hyphens and slashes; 11 digits, the first not 0 (test numbers start with 0 and are rejected).
2. Among digits 1–10 exactly one digit value occurs twice or three times; every other value at most once. A digit that occurs three times may not stand three in a row.
3. Digit 11 is the MOD 11,10 check digit over digits 1–10 (as for `de-vat`).

Examples: `86095742719`, `65929970489`, `86 095 742 719`. Not accepted: `86095742718`, `11123456786` (three in a row), `32658701495` (no repeated digit).

### `de-stnr`

Source: ELSTER, Prüfung der Steuer- und Steueridentifikationsnummer (September 2026), § 3–5.

1. Remove spaces, hyphens and slashes; digits only.
2. 10 or 11 digits (as printed on tax letters) are accepted without further checks: the check digit depends on the Land and tax office.
3. 13 digits (ELSTER format): the 5th digit is 0; the first two digits identify a Land (10, 11, 21–24, 26–28, 30–32, 40, 41, 5x, 9x); the district (digits 6–8, North Rhine-Westphalia 6–9) is not 000, 998 or 999 (NRW: 0000, 0998, 0999); in Bavaria, Brandenburg, Mecklenburg-Western Pomerania, Saarland, Saxony, Saxony-Anhalt and Thuringia the district is at least 100; in NRW digits 10–13 are above 0009; in Bavaria the number does not end in 999 99999.

Examples: `98/815/08152`, `400/8150/8159`, `2866081508156`. Not accepted: `98/815/0815`, `2866181508156`, `3398081508157`.

### `de-trade-register`

Sources: § 8 HGB, Handelsregisterverordnung; MoPeG (Gesellschaftsregister since 2024).

1. Remove spaces; upper-case.
2. Register type `HRA`, `HRB`, `GnR`, `GsR`, `PR` or `VR`, then 1–6 digits not starting with 0, then an optional suffix of 1–3 letters (court or branch marks such as `B` or `HL`).

The court is not part of the number; ask for it in a separate field. Structure only.

Examples: `HRB 12345`, `HRB 12345 B`, `GnR 123`, `GsR 42`. Not accepted: `HRC 12345`, `HRB 012345`, `Amtsgericht Berlin HRB 12345`.

### `de-leitweg`

Source: KoSIT, Leitweg-ID Format-Spezifikation 2.0.2 (2021).

1. Any case. Hyphens are part of the format; no other separators, and no spaces.
2. `Grob[-Fein]-PP`: Grob is 2, 3, 5, 8, 9 or 12 digits and starts with a Land code 01–16 or 99; Fein is 1–30 letters and digits; PP is 2 check digits.
3. ISO 7064 mod 97-10 over Grob, Fein and PP without hyphens, letters read as 10–35: the remainder is 1.

Examples: `04011000-1234512345-06`, `991-03730-19`, `99-92`. Not accepted: `04011000-1234512345-07`, `04011000123451234506`, `55-55-20`, `0401-ABC-00`.

### `de-rvnr`

Source: § 2 VKVV (Versicherungsnummern-, Kontoführungs- und Versicherungsverlaufsverordnung).

1. Remove spaces; upper-case; 12 characters: 2 digits (area), 6 digits (birth date), a letter (initial of the birth name), 2 digits (serial), a check digit.
2. In the first 11 characters, replace the letter by its alphabet position as two digits (A = 01 … Z = 26). This gives 12 digits.
3. Multiply them by 2, 1, 2, 5, 7, 1, 2, 1, 2, 1, 2, 1 and add the digit sums of the products; the sum mod 10 is the check digit.

The birth date and area are not checked (serial overflow moves the day; area numbers are reference data).

Examples: `15070649C103`, `15 070649 C 103`. Not accepted: `15070649C104`, `150706491103`.

### `de-kvnr`

Source: GKV-Spitzenverband, Anlage 1, Prüfziffernberechnung für die Krankenversichertennummer (2023).

1. Remove spaces; upper-case; a letter, 8 digits and a check digit.
2. Replace the letter by its alphabet position as two digits (A = 01 … Z = 26); weights 1, 2, 1, 2, … over the 10 digits; add the digit sums of the products; the sum mod 10 is the check digit.

Examples: `A123456780`, `A000500015`, `a 123 456 780`. Not accepted: `A123456781`, `1123456780`, `AA23456780`.

### `de-id-card` and `de-passport`

Sources: PAuswV Anlage (Abschnitt 1 Nr. 9), PassV Anlage Nr. 9, ICAO Doc 9303 Part 3.

1. Remove spaces; upper-case.
2. 9 characters from `0-9 C F G H J K L M N P R T V W X Y Z`.
3. An optional 10th character is the ICAO 9303 check digit: weights 7, 3, 1 repeating, letters read as 10–35, sum mod 10.

The two names share one rule. The regulations list only the digits 1–9 since November 2021; `0` is accepted because documents issued earlier remain valid. Temporary documents (a letter and 7 digits) are not accepted.

Examples: `T22000129`, `T220001293`, `C01X00T478`. Not accepted: `T220001294`, `A22000129`, `T22O00129`.

### `de-wkn`

Source: WM Datenservice (Wertpapierkennnummer); python-stdnum `de/wkn`.

Remove spaces; upper-case; 6 characters from `0-9 A-H J-N P-Z` (no I or O). No check digit.

Examples: `514000`, `A0MNRK`. Not accepted: `AOMNRK`, `A0MNRK1`, `DE0005140008` (an ISIN).

### `at-vat`

Sources: BMF (UID); python-stdnum `at/uid`.

1. Remove spaces; upper-case; `ATU` and 8 digits.
2. Digits 1, 3, 5 and 7 count as-is; digits 2, 4 and 6 are doubled, minus 9 when above 9. The check digit is (10 − (sum + 4) mod 10) mod 10.

Examples: `ATU13585627`, `ATU 1234 5675`. Not accepted: `ATU12345676`, `U12345675`.

### `at-svnr`

Sources: Dachverband der Sozialversicherungsträger; python-stdnum `at/vnr`.

1. Remove spaces; 10 digits, the first not 0.
2. Weights 3, 7, 9, 0, 5, 8, 4, 2, 1, 6; the sum mod 11 is the 4th digit, and a remainder of 10 is never issued.

The birth date (digits 5–10) is not checked: overflow dates with days above 31 or months above 12 are issued.

Examples: `1237 010180`, `3218320190`. Not accepted: `2237 010180`, `1000010180` (remainder 10).

### `at-fn`

Sources: Firmenbuch check letter (numeric part mod 17, verified against published register numbers); Wikidata P5285.

1. Remove spaces; upper-case; an optional `FN` prefix is removed.
2. 1–6 digits not starting with 0, then a check letter: `ABDFGHIKMPSTVWXYZ`, indexed by the number mod 17.

Examples: `FN 123456d`, `123456d`, `FN122119m`. Not accepted: `FN 123456a`, `FN 123456c`, `FN 012345f`.

### `at-tin`

Sources: OECD, Austria tax identification numbers; python-stdnum `at/tin`.

1. Remove spaces, hyphens and slashes; 9 digits, not all zeros.
2. Luhn over the first 8 digits: digits 2, 4, 6 and 8 are doubled, minus 9 when above 9; the check digit is (10 − sum mod 10) mod 10.

The tax office code is not checked. Examples: `12-345/6782`, `12 3456782`, `59-119/9013`. Not accepted: `12-345/6783`, `12.345.6782`.

### `at-passport`

Source: Stadt Graz, passport information (passports issued from 1 December 2023 have 2 letters). Structure only.

Remove spaces; upper-case; 1 or 2 letters and 7 digits.

Examples: `P1234567`, `AB1234567`. Not accepted: `1234567`, `ABC1234567`, `P-1234567`.

### `li-peid`

Sources: OECD, Liechtenstein tax identification numbers; python-stdnum `li/peid`. Structure only; no check digit.

Remove spaces and dots; at most 12 digits, of which at least 4 remain after leading zeros.

Examples: `1234567`, `0001234567`, `2.580.073`. Not accepted: `123`, `0001`, `12-34567`.

### `ch-uid` and `ch-vat`

Sources: eCH-0097 5.2.0 (§ 2.4.2); UIDG; ESTV.

1. Remove spaces, dots and hyphens; upper-case.
2. `CHE` and 9 digits, not all zeros. Weights 5, 4, 3, 2, 7, 6, 5, 4 on digits 1–8; the check digit is (11 − sum mod 11) mod 11, and 10 is never issued.
3. `ch-uid` accepts nothing after the digits. `ch-vat` accepts nothing or one of `MWST`, `TVA`, `IVA`.

Examples: `CHE-109.322.551`, `CHE-109.322.551 MWST`, `CHE-107.787.577 IVA`. Not accepted: `CHE-109.322.552`, `CHE-444.444.440`, `CHE-109.322.551 VAT`.

### `ch-ahv`

Sources: ZAS and BSV, AHV-Nummer; python-stdnum `ch/ssn`.

Remove spaces and dots; 13 digits starting with `756`; EAN-13 check (weights 1, 3, 1, 3, …; sum divisible by 10).

Examples: `756.1234.5678.97`, `756 1234 5678 97`. Not accepted: `756.1234.5678.98`, `756-1234-5678-97`.

### `ch-qr-reference`

Source: SIX, Swiss Implementation Guidelines for the QR-bill 2.4 (2026), § 2.12.1 and Annex B.

1. Remove spaces; 27 digits, not all zeros.
2. Recursive mod 10 over digits 1–26 with the carry table 0, 9, 4, 6, 8, 2, 7, 1, 3, 5; the check digit is (10 − carry) mod 10.

Examples: `21 00000 00003 13947 14300 09017`. Not accepted: `18 78583` (leading zeros left out), an ISO 11649 creditor reference.

### `ch-qr-iban`

Source: SIX, QR-bill guidelines 2.4, § 2.8 and 2.10.

A valid `iso-13616-iban` for `CH` or `LI` whose institution identification (characters 5–9) is 30000–31999.

Examples: `CH44 3199 9123 0008 8901 2`, `LI44 3000 0000 0000 0000 1`. Not accepted: `CH58 0079 1123 0008 8901 2` (regular IBAN), `CH26 3200 0000 0000 0000 1`.

### `ch-passport`

Sources: fedpol (letters I and O are not used). Plausibility only: no official specification of the number format is published, and the format changed with the 2022/2023 documents.

Remove spaces; upper-case; 8 letters and digits, the first a letter, without I and O.

Examples: `X1234567`, `K1N23A45`. Not accepted: `12345678`, `I1234567`, `X123456`.

### `lu-vat`

Sources: AED; python-stdnum `lu/tva`.

Remove spaces, dots and hyphens; upper-case; `LU` and 8 digits, not all zeros; the last two digits equal the first six mod 89.

Examples: `LU15027442`, `LU 150 274 42`. Not accepted: `LU15027443`, `15027442`, `LU00000000`.

### `lu-matricule`

Source: CCSS, Structure du numéro d'identité (« matricule »); Loi du 19 juin 2013.

1. Remove spaces, dots and hyphens; 13 digits: birth date `YYYYMMDD`, 3 digits, a Luhn check digit and a Verhoeff check digit.
2. Digits 1–11 followed by digit 12 pass the Luhn check; digits 1–11 followed by digit 13 pass the Verhoeff check.

The date is not checked. The 11-digit number used before 2014 is not accepted.

Examples: `1900 0101 001 52`, `1900.0101.999.02`. Not accepted: `1900010100153`, `1900010100125`, `19000101001`.

### `lu-rcs`

Source: Luxembourg Business Registers. Structure only and low confidence: no official definition of the number is published.

Remove spaces; upper-case; a section letter and 1–6 digits, the first not 0.

Examples: `B123456`, `B 123456`, `F123`. Not accepted: `B0123456`, `BB123456`, `B-123456`.

## Adding a format

A format belongs in the catalog only when:

- **Its rule is stable.** The structure and check are fixed by a standard or statute and not expected to change. Rules that shift over time (for example which number ranges are in service) stay out.
- **It checks form, not existence.** The check decides whether a value is well-formed from the value alone. Anything that needs reference data (does this postcode, VAT number or account exist?) is out of scope.

Some formats bend these rules on purpose, because forms ask for the value and a shape check still catches most typos. Their sections say so: the licence plates, `gb-sort-code`, `gb-account-number`, `de-stnr` (10 and 11 digits), `ch-passport` and `lu-rcs` check shape only; `gb-crn` keeps a prefix list and `li-postcode` a range, which change only when the issuing body adds to them.

Steps:

1. Add the check to `ExtendedFormatValidators` (internal) and its name to the catalog table.
2. Port the check to `jsv-runtime.ts` and `jsv-runtime.js` as an exported `isValid…` function, declare it in `TsRuntime.GetDeclarationSource`, and map the name in `JsFormatCodeGenerator` and `TsFormatCodeGenerator` (`MapExtendedFormatToImport`).
3. Add a vector file `TestData/Formats/<name>.json` with valid inputs, invalid inputs, and the documented input variants. The runtime, compiled C#, JS and TS tests all run it.
4. Document the format here: source, rule, accepted input, examples.

Names are `<authority>-<kind>`, where the authority is the body that defines the rule:

- a country, as its ISO 3166-1 alpha-2 code: `nl-bsn`, `nl-vat`, `be-phone`, `gb-vat`;
- a standards body and the number of the standard, for rules that are the same everywhere: `iso-13616-iban`, `iso-9362-bic`, `iso-2108-isbn`. The number identifies the standard; its section in this document states which edition's rule the format implements.

Country codes are two letters and standards bodies are longer, so the two can never collide. A group such as "any EU VAT number" is not a format; combine the national formats with `anyOf`.
