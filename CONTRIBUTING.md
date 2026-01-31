# Contributing

## Building

```bash
dotnet build
```

## Running Tests

```bash
# Run main test suite
dotnet test

# Run stress/fuzzing tests (optional, not run by default)
dotnet test JsonSchemaValidationTests.Stress/JsonSchemaValidationTests.Stress.csproj
```

## Code Coverage

Generate a coverage report using coverlet and ReportGenerator:

```bash
# Install ReportGenerator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage collection
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report
reportgenerator -reports:coverage/**/coverage.cobertura.xml -targetdir:coverage/report -reporttypes:Html

# Open coverage/report/index.html in a browser
```

## Code Quality

The project enforces strict code quality via analyzers. All warnings are treated as errors. Run a build to check for issues:

```bash
dotnet build -c Release
```

## Versioning

This project follows [Semantic Versioning 2.0.0](https://semver.org/).

### Version Format: MAJOR.MINOR.PATCH

**MAJOR version** (breaking changes):
- Removing or renaming public types, methods, or properties
- Changing method signatures (parameters, return types)
- Changing behavior in ways that break existing code
- Dropping support for a JSON Schema draft

**MINOR version** (new features, backwards compatible):
- Adding new public types, methods, or properties
- Adding support for new JSON Schema keywords
- Adding new output formats or validation options
- Deprecating (but not removing) existing APIs

**PATCH version** (bug fixes, backwards compatible):
- Bug fixes that don't change the API
- Performance improvements
- Documentation updates
- Internal refactoring with no API changes

### Pre-release Versions

Pre-release versions use suffixes: `1.0.0-alpha.1`, `1.0.0-beta.2`, `1.0.0-rc.1`

### API Stability

The public API is tracked using `Microsoft.CodeAnalysis.PublicApiAnalyzers`. Any changes to public APIs will be caught at build time, ensuring intentional versioning decisions.
