# FormFinch.JsonSchemaValidation

[![Nightly Quality Gate](https://github.com/formfinch/JsonSchemaValidation/actions/workflows/nightly.yml/badge.svg)](https://github.com/formfinch/JsonSchemaValidation/actions/workflows/nightly.yml)
[![NuGet](https://img.shields.io/nuget/v/FormFinch.JsonSchemaValidation)](https://www.nuget.org/packages/FormFinch.JsonSchemaValidation)

A high-performance JSON Schema validation library for .NET with full draft support, built on `System.Text.Json`.

## Features

- **Draft 2020-12**: Full support for the latest JSON Schema specification, plus backward compatibility with Draft 2019-09, Draft 7, Draft 6, Draft 4, and Draft 3
- **100% spec compliance**: Passes all JSON-Schema-Test-Suite tests
- **High performance**: Optimized for speed with minimal allocations
- **Pure System.Text.Json**: No external JSON library dependencies
- **Output formats**: Flag, Basic, and Detailed output per JSON Schema spec

## Installation

```bash
dotnet add package FormFinch.JsonSchemaValidation
```

## Quick Start

**Schema:**

```json
{
    "type": "object",
    "properties": {
        "name": { "type": "string", "minLength": 1 },
        "age": { "type": "integer", "minimum": 0 },
        "address": {
            "type": "object",
            "properties": {
                "street": { "type": "string" },
                "city": { "type": "string" }
            },
            "required": ["street", "city"]
        }
    },
    "required": ["name", "age"]
}
```

**Valid instance:**

```json
{ "name": "Alice", "age": 30, "address": { "street": "123 Main St", "city": "Springfield" } }
```

**Invalid instance:**

```json
{ "name": "", "age": -1, "address": { "city": 123 } }
```

**Validation:**

```csharp
using FormFinch.JsonSchemaValidation;

// Simple boolean check
var isValid = JsonSchemaValidator.IsValid(schema, valid);

// Flat error list
var basic = JsonSchemaValidator.Validate(schema, invalid);

// Hierarchical errors
var detailed = JsonSchemaValidator.Validate(schema, invalid, OutputFormat.Detailed);
```

## License

This library is dual-licensed:

- **Non-commercial use**: Free under the [PolyForm Noncommercial License 1.0.0](https://github.com/formfinch/JsonSchemaValidation/blob/main/LICENSE)
- **Commercial use**: Requires a [commercial license](https://github.com/formfinch/JsonSchemaValidation/blob/main/COMMERCIAL.md)

See [COMMERCIAL.md](https://github.com/formfinch/JsonSchemaValidation/blob/main/COMMERCIAL.md) for details.

## Code-Gen Targets (Preview)

`jsv-codegen` composes pluggable C#, JavaScript, and TypeScript targets through the central `FormFinch.JsonSchemaValidation.CodeGeneration` contract. Existing command aliases remain available:

```bash
jsv-codegen generate -s schema.json -o ./Generated/ -n MyApp.Generated -c PersonValidator
jsv-codegen generate-js -s schema.json -o ./src/validators/
jsv-codegen generate-ts -s schema.json -o ./src/validators/
```

The JavaScript target emits ESM validators for use in browsers, Node, or bundlers. `generate-js` writes `<schema>.js` (an ESM module exporting `validate(data): boolean`) and a sibling `jsv-runtime.js` (shared helpers and format validators). Import and use:

```js
import validator from "./src/validators/person.js";
if (!validator.validate(data)) { /* reject */ }
```

Current JS/TS scope: Drafts 2020-12, 2019-09, and 4 with target capability checks before emission. See [Known Limitations](https://github.com/formfinch/JsonSchemaValidation/blob/main/KNOWN_LIMITATIONS.md#javascript-code-gen-target-jsv-codegen-generate-js) for the full list of deferred features and behavioral notes.

## Documentation

- [Known Limitations](https://github.com/formfinch/JsonSchemaValidation/blob/main/KNOWN_LIMITATIONS.md) — architectural trade-offs, platform constraints, and compiled validator gaps
- [Contributing](https://github.com/formfinch/JsonSchemaValidation/blob/main/CONTRIBUTING.md) — how to report issues, submit PRs, and code standards
- API docs are provided via XML documentation comments and IntelliSense
- Release history is maintained in GitHub Releases

## Behavior Notes

- `$ref` sibling keywords are draft-specific:
- Draft 7 and earlier: siblings of `$ref` are ignored (masked)
- Draft 2019-09 and 2020-12: siblings of `$ref` are evaluated normally

---

Copyright 2026 FormFinch VOF
