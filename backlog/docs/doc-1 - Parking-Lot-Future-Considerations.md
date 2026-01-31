---
id: doc-1
title: Parking Lot - Future Considerations
type: other
created_date: '2026-01-30 22:00'
---
# Parking Lot - Future Considerations

These items are out of scope for initial release but should be tracked for future work.

## Ideas

- **API documentation site** - DocFX or similar generated API reference
- **Additional target frameworks** - Based on user feedback (see TASK-012 for analysis of .NET 8 effort)
- **Localization** - Error messages in multiple languages
- **VS Code extension** - Schema validation in editor
- **Unify dynamic validators and code generators** - Currently validation logic is implemented twice: once in dynamic validators (e.g., `MinimumValidator.cs`) and again in code generators (e.g., `NumericConstraintsCodeGenerator.cs`). This duplication leads to behavioral divergence bugs when implementations drift apart. Consider refactoring to share core validation logic between both paths, either through shared constants, expression trees, or generating both from a single source of truth.
- **Annotation keyword support** - Collect and propagate schema annotations (`title`, `description`, `default`, `examples`, `deprecated`, `readOnly`, `writeOnly`) in validation output for richer tooling integration
- **DI validation** - Runtime validation that required services are registered correctly, with clear error messages when misconfigured
- **Diagnostics hooks** - Logging, tracing, and metrics integration points for observability in production environments
- **Schema registration error details** - `TryRegisterSchema` currently returns bool; consider richer error information for debugging registration failures

## Notes

These items may become tasks when:
1. User demand materializes
2. Resources become available
3. Strategic priorities shift

To promote an item to a task, use `task_create` with appropriate details.
