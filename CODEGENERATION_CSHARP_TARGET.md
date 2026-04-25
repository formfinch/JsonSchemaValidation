# C# Code Generation Target

Status: Accepted for #38, Date: 2026-04-26

`FormFinch.JsonSchemaValidation.CodeGeneration.CSharp` is the peer target assembly for C# code generation.

## Migration

The previous central generator type:

```csharp
FormFinch.JsonSchemaValidation.CodeGeneration.Generator.SchemaCodeGenerator
```

has moved to:

```csharp
FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator.CSharpSchemaCodeGenerator
```

The target assembly also exposes `CSharpCodeGenerationTarget`, which implements the target-neutral `ICodeGenerationTarget` contract from the central `FormFinch.JsonSchemaValidation.CodeGeneration` assembly.

## Package Status

The C# target project is structured as a packageable assembly, but publishing `FormFinch.JsonSchemaValidation.CodeGeneration.CSharp` as an independent NuGet package is deferred to release preparation. Until then, repository projects consume it by project reference.
