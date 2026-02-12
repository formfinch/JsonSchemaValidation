---
id: TASK-74
title: Ship jsv-codegen as a dotnet tool NuGet package
status: To Do
assignee: []
created_date: '2026-02-11 20:37'
updated_date: '2026-02-11 21:05'
labels:
  - packaging
  - tooling
dependencies:
  - TASK-68
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Add PackAsTool properties and package metadata to JsonSchemaValidation.CodeGenerator.csproj so it can be published to NuGet as a dotnet tool. Customers install via `dotnet tool install -g FormFinch.JsonSchemaValidation.CodeGenerator` and run `jsv-codegen` to generate or compile validators for their schemas.

Commands:
- `jsv-codegen generate` — generates .cs source file from a schema (existing)
- `jsv-codegen compile` — generates .cs and compiles it to a loadable DLL (new). This enables runtime scenarios where a server receives user-defined schemas, shells out to jsv-codegen to produce a DLL, and loads it via AssemblyLoadContext. All Roslyn/codegen memory is reclaimed when the process exits.
- `jsv-codegen analyze` — analyzes schema for compilation compatibility (existing)

Includes: PackAsTool, ToolCommandName, package ID, version, description, license, project URL, repository URL. Also needs to be added to the release workflow (published after the main library package).

CodeGeneration stays as a project reference and gets bundled into the tool package. Only two public packages: FormFinch.JsonSchemaValidation (library) and FormFinch.JsonSchemaValidation.CodeGenerator (tool).
<!-- SECTION:DESCRIPTION:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Release ordering: FormFinch.JsonSchemaValidation must be published to NuGet before the tool package, since jsv-codegen depends on it transitively (CodeGenerator → CodeGeneration → JsonSchemaValidation). The release workflow needs to handle this — either publish both in sequence in one workflow, or publish the library first and the tool as a follow-up.

Packaging approach: CodeGeneration stays as a project reference and gets bundled into the tool package automatically (dotnet tools embed all project dependencies). No need to publish CodeGeneration as a separate NuGet package. Only two public packages: FormFinch.JsonSchemaValidation (library) and FormFinch.JsonSchemaValidation.CodeGenerator (tool).

New `compile` command: takes a schema JSON file, generates C# via SchemaCodeGenerator, compiles to a DLL via Roslyn, outputs the DLL path. This replaces the need for RuntimeValidatorFactory as an in-process library — servers shell out to jsv-codegen compile, load the resulting DLL, and register the validator. All Roslyn memory is cleaned up when the external process exits.

The `compile` command needs Roslyn (Microsoft.CodeAnalysis.CSharp). Currently that dependency is in the Compiler project. For the tool, add it directly as a PackageReference in CodeGenerator.csproj. The compilation logic from RuntimeValidatorFactory.CompileToAssembly can be moved/adapted into the tool. Since it's a CLI tool, the Roslyn dependency is fine — it's not pulled into customer apps.
<!-- SECTION:NOTES:END -->
