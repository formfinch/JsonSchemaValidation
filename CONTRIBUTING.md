# Contributing to FormFinch.JsonSchemaValidation

Thank you for your interest in contributing! We welcome bug reports, feature suggestions, and pull requests.

## License Agreement

This project uses a dual-license model:

- **Non-commercial use**: Free under the [PolyForm Noncommercial License 1.0.0](LICENSE)
- **Commercial use**: Requires a [paid commercial license](COMMERCIAL.md)

**By submitting a contribution (pull request, patch, or any other form), you agree to the following:**

1. You grant FormFinch VOF a perpetual, worldwide, irrevocable, royalty-free license to use, modify, sublicense, and distribute your contribution under any license, including commercial licenses.
2. You confirm that you have the right to make the contribution and that it does not infringe any third-party rights.
3. Your contribution will be made available to the public under the [PolyForm Noncommercial License 1.0.0](LICENSE), and may also be included in commercially licensed versions of the software.

This agreement applies in place of a formal Contributor License Agreement (CLA). If you have questions, contact **support@formfinch.com**.

## How to Contribute

### Reporting Issues

- Search [existing issues](https://github.com/formfinch/JsonSchemaValidation/issues) before opening a new one.
- Include a minimal reproducing schema and instance when reporting validation bugs.
- Mention the JSON Schema draft version if relevant.

### Suggesting Features

Before investing time in a new feature, [open an issue](https://github.com/formfinch/JsonSchemaValidation/issues) to discuss your idea first. This helps ensure the feature aligns with the project's direction and avoids duplicate effort.

### Submitting Pull Requests

1. For bug fixes, feel free to open a PR directly. For new features, discuss in an issue first (see above).
2. Fork the repository and create a feature branch from `main`.
3. Make your changes on the feature branch.
4. Ensure the build passes with zero warnings and all tests pass (see [Building & Testing](#building--testing)).
5. Open a pull request against `main` with a clear description of the change.

Keep pull requests focused on a single change. Avoid bundling unrelated fixes or formatting changes.

## Code Standards

- The build enforces `TreatWarningsAsErrors` with multiple analyzers enabled. Your code must compile with zero warnings.
- Nullable reference types are enabled. Do not suppress nullable warnings without justification.
- Match the existing code style. Do not reformat code outside the scope of your change.
- Add or update tests for any behavior changes. The [JSON-Schema-Test-Suite](https://github.com/json-schema-org/JSON-Schema-Test-Suite) is included as a submodule for spec compliance data — write new tests for features and bug fixes in separate test classes.

## Building & Testing

```bash
# Build
dotnet build

# Run all tests
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj

# Run tests for a specific draft
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj --filter "Draft=2020-12"

# Run stress/fuzzing tests (optional, not run by default)
dotnet test JsonSchemaValidationTests.Stress/JsonSchemaValidationTests.Stress.csproj
```

### Code Coverage

```bash
# Install ReportGenerator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage collection
dotnet test JsonSchemaValidationTests/JsonSchemaValidationTests.csproj --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report
reportgenerator -reports:coverage/**/coverage.cobertura.xml -targetdir:coverage/report -reporttypes:Html
```

## Versioning

This project follows [Semantic Versioning 2.0.0](https://semver.org/). The public API is tracked using `Microsoft.CodeAnalysis.PublicApiAnalyzers` — any changes to public APIs will be caught at build time.

---

*Questions? Open an issue or email **support@formfinch.com**.*
