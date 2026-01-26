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
