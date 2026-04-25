# CodeGeneration Target Contract

Status: Accepted
Date: 2026-04-25
Related: #35, #36

## Context

The current CodeGeneration setup has three different shapes for the supported targets:

- C# generation lives in the central `FormFinch.JsonSchemaValidation.CodeGeneration` assembly.
- JavaScript generation has a target assembly, but its shape is not modeled through a shared target contract.
- TypeScript generation lives under the JavaScript project and namespace.

The target split in #35 needs the central assembly to define only target-neutral schema analysis and orchestration contracts. C#, JavaScript, and TypeScript should each become peer pluggable target assemblies.

This note resolves the design gates from #36 so #37 can implement the central abstractions without re-opening the contract shape.

## Decisions

### Central Target Contract

The central package should expose a target-neutral contract along these lines:

```csharp
public interface ICodeGenerationTarget
{
    CodeGenerationTargetDescriptor Descriptor { get; }

    ValueTask<CodeGenerationCapabilityResult> GetCapabilitiesAsync(
        CodeGenerationRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<CodeGenerationResult> GenerateAsync(
        CodeGenerationRequest request,
        CancellationToken cancellationToken = default);
}
```

The exact namespace is decided in #37, but it should be under the central `FormFinch.JsonSchemaValidation.CodeGeneration` assembly and must not mention C#, JavaScript, or TypeScript concepts.

`CodeGenerationTargetDescriptor` should include:

- stable target id, for example `csharp`, `javascript`, or `typescript`
- display name
- supported file extensions
- concrete target options type as a `Type`
- supported draft list

Target discovery is explicit. The CLI and application composition roots register known targets directly or through target-owned extension methods. The central package should not scan assemblies or use reflection-based auto-discovery.

The contract is async-first. Targets that only do CPU-bound source generation may return completed `ValueTask` instances. Targets that call toolchains or perform file/process work can use the same contract without adding a second API shape later.

### Request Model

Introduce `CodeGenerationRequest` as the symmetric input model for `CodeGenerationResult`.

The request should contain:

- schema input, represented as an already parsed `JsonElement`
- source path, when the schema came from a file or another named source
- default draft to use when the schema does not declare `$schema`
- output naming hints, such as base file name, namespace, class name, or module name as applicable
- `EmitSupportArtifacts`, a common boolean option that maps CLI behavior such as `--no-runtime`
- target options, represented by a concrete target-specific options object

The central target contract should not read schema files. File loading belongs to callers such as the CLI, tests, or package-specific convenience APIs. Target assemblies may expose convenience overloads that accept paths, but the central contract should operate on parsed schema input to avoid mixing orchestration with I/O.

Draft detection should return a structured value, not only a draft enum. It should distinguish an explicit `$schema` match from a fallback default and retain the source URI or fallback reason so diagnostics can explain how the draft was selected.

### Result And Artifact Model

Do not expand the current single-file `GenerationResult` into the central target contract. Introduce a new multi-artifact `CodeGenerationResult`.

The result should contain:

- success flag
- generated artifacts
- diagnostics

Generated artifacts should carry:

- file name or relative path
- generated text content
- artifact kind, such as source, runtime, declaration, metadata, or source map
- target language or media type when useful
- primary/support role

Keep both artifact kind and role. Kind describes what the artifact is; role describes how callers should treat it. They usually align, but they can diverge when a target emits multiple primary source files or a metadata file that is primary for a downstream build step.

The existing `GenerationResult` may remain temporarily only as a compatibility or target-internal type while phases migrate. New cross-target code should use `CodeGenerationResult`.

Diagnostics use one shared `CodeGenerationDiagnostic` type across capability and generation results so callers can surface preflight warnings and generation failures uniformly.

### Capability Result Shape

Introduce `CodeGenerationCapabilityResult` so callers can ask whether a target can handle a schema before generation.

The result should contain:

- `CanGenerate`
- structured draft selection, when available
- diagnostics
- unsupported draft details
- unsupported feature details

Diagnostics should have a severity model:

- error: generation should not proceed unless the caller explicitly chooses to ignore preflight
- warning: generation can proceed, but behavior or portability may be limited
- info: explanatory capability detail

Unsupported feature details should include the target id, keyword or feature name, JSON Pointer location when known, schema draft when relevant, and a concise reason.

`GetCapabilitiesAsync` should perform draft and feature analysis only. It should not emit files or require target toolchains such as Node or `tsc`.

### Compatibility Stance

Treat `SchemaCodeGenerator` as a breaking pre-1.0 migration, not as a permanent central compatibility facade.

The C# generator should move to the C# target assembly and be renamed to `CSharpSchemaCodeGenerator`. The central package must not keep a facade that depends on the C# target, because that would preserve the current central-to-C# coupling.

Migration notes should point existing users from:

```csharp
FormFinch.JsonSchemaValidation.CodeGeneration.Generator.SchemaCodeGenerator
```

to the C# target package/type selected in #38.

`JsonSchemaValidation.Compiler` is inherently a C# runtime-compilation package, so it should reference the C# target assembly directly rather than forcing the central package to know about C#.

### Options Model

Use strongly typed target options.

Use a non-generic public `ICodeGenerationTarget` so the CLI and registry can store C#, JavaScript, and TypeScript targets in one collection. The request carries a target-specific options instance derived from a shared base type, and `CodeGenerationTargetDescriptor.OptionsType` exposes the required concrete type.

#37 may add a typed base/helper such as `CodeGenerationTarget<TOptions>` to validate and cast options once, but the public registry-facing interface should remain non-generic.

The central base options type should contain only common fields:

- source path
- default draft
- output naming hints
- `EmitSupportArtifacts`

Each target should define its own options type for target-specific settings:

- C#: namespace, class name, generated-regex behavior, forced annotation tracking
- JavaScript: runtime import specifier, annotation tracking behavior
- TypeScript: runtime import specifier, annotation tracking behavior, compile/transpile settings if needed

Avoid a generic string/object option bag in the library contract. The CLI may parse arguments dynamically, but it should map them into strongly typed options before invoking a target.

### Runtime And Support Artifacts

Runtime/support files are target-owned generated artifacts.

Targets should return runtime files, declarations, and other support files in `CodeGenerationResult.Artifacts` when `EmitSupportArtifacts` is true. The CLI should write returned artifacts consistently instead of duplicating per-target runtime-writing logic in `Program.cs`.

The existing `--no-runtime` behavior should map to `EmitSupportArtifacts = false` on the central base options. Targets still own the actual runtime file names and contents.

For the TypeScript split, the current TypeScript runtime may continue to derive from the JavaScript runtime during #39. Ownership and deduplication between JS and TS runtime sources remains deferred to #42.

## Phase Implications

#37 should add the target-neutral contract and move shared schema analysis types into a neutral namespace/location. It should not add new central abstractions that reference C#, JavaScript, TypeScript, or target-specific keyword emitters.

#38 should move the C# generator and C# keyword emitters into `FormFinch.JsonSchemaValidation.CodeGeneration.CSharp`, introduce `CSharpSchemaCodeGenerator`, and update `JsonSchemaValidation.Compiler` to depend on that C# target assembly directly.

#39 should move TypeScript generation into `FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript` while preserving the optional Node/`tsc` story. TypeScript may temporarily reference JavaScript runtime code until #42.

#40 should normalize JavaScript as an adapter over the existing direct JavaScript generator. JS-from-TS routing is out of scope and belongs to #42.

#41 should compose the CLI from registered targets and write artifacts according to this contract. It should keep the existing command aliases unless that issue is explicitly amended.

#42 remains a deferred design issue for JS/TS deduplication, including the 2026-05-15 TypeScript migration decision gate documented in `TYPESCRIPT_CODEGEN_MIGRATION.md`.
