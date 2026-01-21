# FormFinch.JsonSchemaValidation

A high-performance JSON Schema validation library for .NET with full draft support, built on `System.Text.Json` with zero external dependencies.

## Features

- **Full draft support**: Draft 3, Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
- **100% spec compliance**: Passes all JSON-Schema-Test-Suite tests
- **High performance**: Optimized for speed with minimal allocations
- **Pure System.Text.Json**: No Newtonsoft.Json or other JSON library dependencies
- **Output formats**: Flag, Basic, and Detailed output per JSON Schema spec

## Installation

```bash
dotnet add package FormFinch.JsonSchemaValidation
```

## Quick Start

```csharp
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Repositories;
using System.Text.Json;

// Set up dependency injection
var services = new ServiceCollection();
services.AddSchemaValidation();
var provider = services.BuildServiceProvider();

// Get the schema repository and register a schema
var repository = provider.GetRequiredService<ISchemaRepository>();
var schema = JsonDocument.Parse("""
{
    "type": "object",
    "properties": {
        "name": { "type": "string" },
        "age": { "type": "integer", "minimum": 0 }
    },
    "required": ["name"]
}
""");
repository.RegisterSchema("https://example.com/person.json", schema.RootElement);

// Validate a document
var validator = repository.GetValidator("https://example.com/person.json");
var document = JsonDocument.Parse("""{ "name": "Alice", "age": 30 }""");

var result = validator.Validate(document.RootElement);
Console.WriteLine($"Valid: {result.IsValid}");
```

## License

This library is dual-licensed:

- **Non-commercial use**: Free under the [PolyForm Noncommercial License 1.0.0](LICENSE)
- **Commercial use**: Requires a [commercial license](COMMERCIAL.md)

See [COMMERCIAL.md](COMMERCIAL.md) for pricing and terms.

## Documentation

Full documentation coming soon.

---

Copyright 2024 FormFinch VOF
