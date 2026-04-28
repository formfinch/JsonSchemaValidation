# TypeScript-First JavaScript Codegen Migration

## Current State

The direct JavaScript generator remains the default path and is still the benchmark baseline:

```bash
jsv-codegen generate-js -s schema.json -o ./out
```

A parallel TypeScript-first path now exists:

```bash
jsv-codegen generate-ts -s schema.json -o ./out
jsv-codegen generate-js -s schema.json -o ./out --pipeline typescript --ecmascript-target ES2020
```

The TypeScript path has its own orchestration, capability gate, reachability pass, context, literal helper, and keyword generator classes. The first implementation deliberately keeps generated validator semantics comparable with the direct JavaScript emitter so parity and performance comparisons remain meaningful.

The TypeScript generator now lives in the peer target assembly `FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript`. The JavaScript target assembly no longer contains the TypeScript generator namespace or source folder.

## Source Of Truth

Short term, the canonical source remains the current schema analysis and the TS keyword emitter set. The TS path emits TypeScript-compatible ESM source with typed exported validator signatures.

Medium term, the generator should move toward one of these models:

- A target-neutral validation IR that can emit C#, TS, and any future target.
- A TS-native emitter that becomes the canonical JS-family output, with direct JS retained only until parity and performance are proven.

The IR option is cleaner long term, but larger. The current implementation preserves the current JS generator for performance comparison while giving TypeScript its own generator implementation.

## Runtime

`jsv-runtime.ts` is authored TypeScript. It exports the shared runtime helpers plus concrete ABI types for JSON values, validator modules, registry handles, dynamic scope, and evaluated-state tracking. The runtime source must not contain `// @ts-nocheck`.

The TypeScript target assembly no longer depends on `FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript` for runtime projection. Focused tests compile the runtime with `strict: true` and `noImplicitAny: true`; generated validator modules still use the broader migration compiler settings until the remaining internal `any` state/scope/registry signatures are replaced.

## Toolchain

No Node dependency is added to the .NET projects. The CLI invokes an external `tsc` executable for the TS-to-JS path. Consumers can either install TypeScript on PATH or pass `--tsc <path>`.

The default direct JS path does not require Node or TypeScript.

## tsc Invocation Point

Current behavior:

- `generate-ts`: development and inspection path; writes `.ts` source and `jsv-runtime.ts`.
- `generate-js --pipeline typescript`: packaging/output path; writes temporary `.ts` source, invokes `tsc`, and emits `.js`.
- `tsc` is invoked with `--downlevelIteration` so lower ECMAScript targets can compile runtime iteration over `Map`, `Set`, and `Intl.Segmenter` results. This can add TypeScript helper code to downlevel output and should be considered in benchmark and delivery-size reviews.
- tests: focused smoke coverage compiles and executes generated TS validators, and `TsTestSuiteRunner` runs Draft 2020-12 JSON-Schema-Test-Suite cases through TS-derived JS.
- benchmarks: `NodeJsCompetitorBenchmarks` now compares Ajv, direct JS codegen, and TS-derived JS codegen.

## ECMAScript Target Exposure

The CLI exposes `--ecmascript-target <target>` only for `--pipeline typescript`. The value is passed through to `tsc --target`, so supported values follow the installed TypeScript compiler.

Direct JS generation rejects `--ecmascript-target` to avoid implying that the direct emitter has per-target downleveling.

## Explicitly Out Of Scope

- Browser-query support.
- Built-in polyfill selection or injection.
- Replacing the direct JS generator before TS parity and benchmark parity are demonstrated.
- Treating current compiled-generator coverage gaps as permanently out of scope.

## Next Gates

- Track unsupported or divergent cases separately instead of masking them as migration blockers.
- By 2026-05-15, choose the deduplication strategy for the JS-family emitters: either introduce a target-neutral validation IR or promote the TS emitter to the canonical JS-family source and retire duplicated direct-JS keyword bodies behind benchmark gates.
- Replace internal `any` state/scope/registry signatures with typed TS contracts.
- Enable strict generated-validator compiler gates once those internal signatures are typed.
- Add benchmark acceptance thresholds once enough TS-derived JS scenarios are stable.
