# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

JsonSchemaValidation is a .NET class library implementing JSON Schema Draft 2020-12 validation using pure `System.Text.Json` (no external JSON dependencies). The library uses a plugin-based architecture with dependency injection for extensibility.

## Build Commands

```bash
# Build the solution
dotnet build

# Build specific configuration
dotnet build -c Release
```

## Running Tests

The project uses xUnit with 512+ tests (448 from JSON-Schema-Test-Suite submodule + 64 output format tests).

```bash
# Run all tests
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj

# Run specific test by name
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run with verbosity
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj -v normal
```

## Architecture

### Core Components

1. **Dependency Injection Layer** (`DependencyInjection/`)
   - `SchemaValidationSetup.cs` - Main DI configuration entry point
   - `SchemaValidationOptions` - Configuration (e.g., `FormatAssertionEnabled`)

2. **Schema Management** (`Repositories/`)
   - `SchemaRepository.cs` - Thread-safe schema registry using `ConcurrentDictionary`
   - Handles schema registration, URI resolution, and `$id` management

3. **Factory Pattern** (`Draft202012/Keywords/`)
   - 49 keyword-specific validator factories
   - Each keyword has a `*Validator.cs` implementing `IKeywordValidator` and a `*ValidatorFactory.cs` implementing `ISchemaDraftKeywordValidatorFactory`

4. **Validation Context** (`Common/`)
   - `IJsonValidationContext` - Encapsulates JSON values being validated
   - `ValidationScope` - Tracks scope for `$dynamicRef` resolution
   - `ScopeAwareSchemaValidator` - Manages dynamic scope during validation

5. **Validation Engine** (`Validation/`)
   - `SchemaValidator.cs` - Orchestrates keyword validators
   - `ValidationResult.cs` - Contains `IsValid`, `Errors`, and `Annotations`

### Keyword Categories in Draft 2020-12

- **Logic:** `AllOf`, `AnyOf`, `OneOf`, `Not`, `IfThenElse`
- **Applicator:** `Properties`, `Items`, `PrefixItems`, `PatternProperties`, `DependentSchemas`
- **Validation:** `Type`, `Enum`, `Const`, `Pattern`, `Format`, numeric constraints
- **Core:** `$ref`, `$dynamicRef`, `$anchor`, `$dynamicAnchor`, `$id`
- **Unevaluated:** `UnevaluatedItems`, `UnevaluatedProperties`

### Format Validators (`Draft202012/Keywords/Format/`)

19 format validators including: `date-time`, `date`, `time`, `email`, `hostname`, `ipv4`, `ipv6`, `uri`, `uuid`, etc. Features ECMAScript regex compatibility, IDN support, and leap second handling.

## Key Architectural Notes

1. **Validator Ordering**: Validators are sorted by `ExecutionOrder` property (default 0). `UnevaluatedItems` and `UnevaluatedProperties` use `ExecutionOrder = 100` to run last. This is enforced in `SchemaDraft202012ValidatorFactory` constructor.

2. **Dynamic Scope**: `$dynamicRef` and `$dynamicAnchor` require scope tracking via `ValidationScope` and `ScopeAwareSchemaValidator`.

3. **Remote Schemas**: Must be explicitly registered in `SchemaRepository` before use. Tests pre-load them from `submodules/JSON-Schema-Test-Suite/remotes/`.

4. **Thread Safety**: Schema registry uses `ConcurrentDictionary`; all validators registered as singletons.

## Test Infrastructure

- Tests use JSON-Schema-Test-Suite (git submodule at `submodules/JSON-Schema-Test-Suite/`)
- `TestCaseLoader.cs` dynamically loads test cases from JSON files
- Two service provider configurations: format annotation-only (default) and format assertion enabled

## Output Format Support

The library implements spec-compliant output formats per JSON Schema 2020-12 Section 12:

- **Flag** - Boolean only (most efficient)
- **Basic** - Flat list of errors with instance/keyword locations
- **Detailed** - Hierarchical nested structure with annotations

Key classes:
- `Validation/Output/OutputUnit.cs` - Spec-compliant output structure
- `Validation/Output/OutputFormat.cs` - Format enum
- `Common/JsonPointer.cs` - RFC 6901 JSON Pointer implementation

Usage:
```csharp
var output = validator.ValidateFlag(context);   // Just valid/invalid
var output = validator.ValidateBasic(context);  // Flat error list
var output = validator.ValidateDetailed(context); // Hierarchical
```

## Test Structure

Tests are organized by feature using a feature-first folder structure:

```
JsonSchemaValidationTests/Draft202012/
├── SchemaValidationTests.cs    # JSON-Schema-Test-Suite tests
└── OutputFormat/
    ├── Examples.cs             # Demonstration tests (11 tests)
    └── RegressionTests.cs      # Comprehensive coverage (53 tests)
```

## Known Gaps (from DRAFT_2020_12_COMPLIANCE.md)

1. **Cross-Draft Compatibility**: Not supported (low priority/optional)

## Target Frameworks

net9.0, net8.0, net6.0
