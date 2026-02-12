# FormFinch.JsonSchemaValidation

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

```csharp
using FormFinch.JsonSchemaValidation;

var schema = """
{
    "type": "object",
    "properties": {
        "name": { "type": "string", "minLength": 1 },
        "age": { "type": "integer", "minimum": 0 }
    },
    "required": ["name", "age"]
}
""";

// Simple boolean check
var isValid = JsonSchemaValidator.IsValid(schema, """{ "name": "Alice", "age": 30 }""");

// Validate with error details
var result = JsonSchemaValidator.Validate(schema, """{ "name": "", "age": -1 }""");

// Hierarchical errors
var detailed = JsonSchemaValidator.Validate(schema, """{ "name": "", "age": -1 }""", OutputFormat.Detailed);
```

## License

This library is dual-licensed:

- **Non-commercial use**: Free under the [PolyForm Noncommercial License 1.0.0](LICENSE)
- **Commercial use**: Requires a [commercial license](COMMERCIAL.md)

See [COMMERCIAL.md](COMMERCIAL.md) for details.

## Documentation

Full documentation coming soon.

---

Copyright 2026 FormFinch VOF
