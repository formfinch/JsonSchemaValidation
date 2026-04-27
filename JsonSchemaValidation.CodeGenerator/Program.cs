// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;
using FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.CodeGenerator;

internal static class Program
{
    private const string CSharpTargetId = "csharp";
    private const string JavaScriptTargetId = "javascript";
    private const string TypeScriptTargetId = "typescript";

    private static async Task<int> Main(string[] args)
    {
        return await RunAsync(args);
    }

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var targets = CreateTargetRegistry();
        var command = args[0].ToLowerInvariant();
        var remainingArgs = args.Skip(1).ToArray();

        return await (command switch
        {
            "generate" => HandleGenerateAsync(remainingArgs, targets),
            "generate-js" => HandleGenerateJsAsync(remainingArgs, targets),
            "generate-ts" => HandleGenerateTsAsync(remainingArgs, targets),
            "generate-metaschemas" => HandleGenerateMetaschemasAsync(remainingArgs, targets),
            "compile-test-schemas" => HandleCompileTestSchemasAsync(remainingArgs, targets),
            "analyze" => Task.FromResult(HandleAnalyze(remainingArgs)),
            "--help" or "-h" or "help" => Task.FromResult(PrintUsage()),
            _ => Task.FromResult(PrintUsage())
        });
    }

    private static int PrintUsage()
    {
        Console.WriteLine("""
            JSON Schema Validator Code Generator

            Usage: jsv-codegen <command> [options]

            Commands:
              generate              Generate compiled C# validator from schema file
              generate-js           Generate compiled JavaScript (ESM) validator from schema file
              generate-ts           Generate compiled TypeScript (ESM) validator from schema file
              generate-metaschemas  Generate compiled validators for all metaschemas
              compile-test-schemas  Generate compiled validators for JSON-Schema-Test-Suite schemas
              analyze               Analyze schema for compilation compatibility

            generate options:
              -s, --schema <path>     Input schema file (required)
              -o, --output <path>     Output directory for generated code (required)
              -n, --namespace <name>  Namespace for generated classes (default: JsonSchemaValidation.Generated)
              -c, --class <name>      Class name (defaults to derived from schema $id)

            generate-js options:
              -s, --schema <path>    Input schema file (required)
              -o, --output <path>    Output directory (required). Emits <schema>.js and jsv-runtime.js.
              --pipeline <mode>      JS emission pipeline: direct (default) or typescript.
              --ecmascript-target <target>
                                     tsc target for --pipeline typescript (default: ES2020).
              --tsc <path>           TypeScript compiler executable for --pipeline typescript (default: tsc).
              --assert-format       Assert supported format values for Draft 2020-12.
              --no-runtime           Skip writing jsv-runtime.js (useful when runtime is already present).

            generate-ts options:
              -s, --schema <path>    Input schema file (required)
              -o, --output <path>    Output directory (required). Emits <schema>.ts and jsv-runtime.ts.
              --assert-format        Assert supported format values for Draft 2020-12.
              --no-runtime           Skip writing jsv-runtime.ts.

            generate-js supported scope:
              - Drafts: 2020-12, 2019-09, and 4 (other drafts rejected pre-emission).
              - Local $ref is inlined; external $ref resolves through an optional JS registry
                passed to validate(data, registry). Missing registry or missing entries fail validation.
              - Annotation tracking: unevaluatedProperties/items are supported under Drafts
                2020-12 and 2019-09 via generated evaluated-state tracking.
              - Dynamic refs: $dynamicRef/$dynamicAnchor are supported under Draft 2020-12.
                $recursiveRef/$recursiveAnchor (Draft 2019-09) are still deferred and are
                rejected pre-emission in drafts that define them.
              - Format: Draft 4 asserts supported formats by default; Draft 2020-12 is
                annotation-only unless --assert-format is set.

            generate-metaschemas options:
              -o, --output <path>     Output directory for generated code (required)
              -l, --lib-path <path>   Path to JsonSchemaValidation project (required)

            compile-test-schemas options:
              -t, --test-suite <path> Path to JSON-Schema-Test-Suite directory (required)
              -o, --output <path>     Output directory for generated code (required)
              -n, --namespace <name>  Namespace for generated classes (default: JsonSchemaValidationBenchmarks.Generated)
              -d, --draft <version>   Draft version to compile (default: all). Options: 2020-12, 2019-09, 7, 6, 4, 3

            analyze options:
              -s, --schema <path>     Input schema file to analyze (required)

            Examples:
              jsv-codegen generate -s schema.json -o ./Generated/
              jsv-codegen generate-js -s schema.json -o ./src/validators/
              jsv-codegen generate-js -s schema.json -o ./src/validators/ --pipeline typescript --ecmascript-target ES2020
              jsv-codegen generate-ts -s schema.json -o ./src/validators/
              jsv-codegen generate-metaschemas -o ./Generated/ -l ../JsonSchemaValidation
              jsv-codegen compile-test-schemas -t ./submodules/JSON-Schema-Test-Suite -o ./Generated/
              jsv-codegen analyze -s schema.json
            """);
        return 1;
    }

    private static async Task<int> HandleGenerateAsync(
        string[] args,
        IReadOnlyDictionary<string, ICodeGenerationTarget> targets)
    {
        string? schemaPath = null;
        string? outputPath = null;
        var namespaceName = "JsonSchemaValidation.Generated";
        string? className = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var nextArg = i + 1 < args.Length ? args[i + 1] : null;

            switch (arg)
            {
                case "-s":
                case "--schema":
                    schemaPath = nextArg;
                    i++;
                    break;
                case "-o":
                case "--output":
                    outputPath = nextArg;
                    i++;
                    break;
                case "-n":
                case "--namespace":
                    namespaceName = nextArg ?? namespaceName;
                    i++;
                    break;
                case "-c":
                case "--class":
                    className = nextArg;
                    i++;
                    break;
            }
        }

        if (string.IsNullOrEmpty(schemaPath))
        {
            Console.Error.WriteLine("Error: Schema path is required (-s, --schema)");
            return 1;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.Error.WriteLine("Error: Output path is required (-o, --output)");
            return 1;
        }

        if (!File.Exists(schemaPath))
        {
            Console.Error.WriteLine($"Error: Schema file not found: {schemaPath}");
            return 1;
        }

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        Console.WriteLine($"Generating validator for: {schemaPath}");
        Console.WriteLine($"Output directory: {outputPath}");
        Console.WriteLine($"Namespace: {namespaceName}");

        var options = new CSharpCodeGenerationOptions
        {
            SourcePath = schemaPath,
            UseGeneratedRegex = true,
            OutputHints = new CodeGenerationOutputHints
            {
                NamespaceName = namespaceName,
                TypeName = className
            }
        };

        return await GenerateWithTargetAsync(targets, CSharpTargetId, schemaPath, outputPath, options);
    }

    private static async Task<int> HandleGenerateJsAsync(
        string[] args,
        IReadOnlyDictionary<string, ICodeGenerationTarget> targets)
    {
        string? schemaPath = null;
        string? outputPath = null;
        var writeRuntime = true;
        var formatAssertionEnabled = false;
        var pipeline = "direct";
        var ecmaScriptTarget = "ES2020";
        var tscExecutable = "tsc";
        var ecmaScriptTargetSpecified = false;
        var tscSpecified = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var nextArg = i + 1 < args.Length ? args[i + 1] : null;
            switch (arg)
            {
                case "-s":
                case "--schema":
                    if (!IsOptionValue(nextArg))
                    {
                        Console.Error.WriteLine($"Error: Option {arg} requires a value.");
                        return 1;
                    }
                    schemaPath = nextArg;
                    i++;
                    break;
                case "-o":
                case "--output":
                    if (!IsOptionValue(nextArg))
                    {
                        Console.Error.WriteLine($"Error: Option {arg} requires a value.");
                        return 1;
                    }
                    outputPath = nextArg;
                    i++;
                    break;
                case "--no-runtime":
                    writeRuntime = false;
                    break;
                case "--assert-format":
                    formatAssertionEnabled = true;
                    break;
                case "--pipeline":
                    if (!IsOptionValue(nextArg))
                    {
                        Console.Error.WriteLine($"Error: Option {arg} requires a value.");
                        return 1;
                    }
                    pipeline = nextArg!;
                    i++;
                    break;
                case "--ecmascript-target":
                    if (!IsOptionValue(nextArg))
                    {
                        Console.Error.WriteLine($"Error: Option {arg} requires a value.");
                        return 1;
                    }
                    ecmaScriptTarget = nextArg!;
                    ecmaScriptTargetSpecified = true;
                    i++;
                    break;
                case "--tsc":
                    if (!IsOptionValue(nextArg))
                    {
                        Console.Error.WriteLine($"Error: Option {arg} requires a value.");
                        return 1;
                    }
                    tscExecutable = nextArg!;
                    tscSpecified = true;
                    i++;
                    break;
            }
        }

        if (string.IsNullOrEmpty(schemaPath))
        {
            Console.Error.WriteLine("Error: Schema path is required (-s, --schema)");
            return 1;
        }
        if (string.IsNullOrEmpty(outputPath))
        {
            Console.Error.WriteLine("Error: Output path is required (-o, --output)");
            return 1;
        }
        if (!File.Exists(schemaPath))
        {
            Console.Error.WriteLine($"Error: Schema file not found: {schemaPath}");
            return 1;
        }
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        Console.WriteLine($"Generating JS validator for: {schemaPath}");
        Console.WriteLine($"Output directory: {outputPath}");

        if (string.Equals(pipeline, "typescript", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleGenerateJsFromTypeScriptAsync(
                targets,
                schemaPath,
                outputPath,
                writeRuntime,
                formatAssertionEnabled,
                ecmaScriptTarget,
                tscExecutable);
        }

        if (!string.Equals(pipeline, "direct", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Error: Unsupported JS generation pipeline: {pipeline}. Expected 'direct' or 'typescript'.");
            return 1;
        }

        if (ecmaScriptTargetSpecified || tscSpecified)
        {
            var optionNames = new List<string>();
            if (ecmaScriptTargetSpecified) optionNames.Add("--ecmascript-target");
            if (tscSpecified) optionNames.Add("--tsc");
            var verb = optionNames.Count == 1 ? "requires" : "require";
            Console.Error.WriteLine($"Error: {string.Join(" and ", optionNames)} {verb} --pipeline typescript.");
            return 1;
        }

        var options = new JavaScriptCodeGenerationOptions
        {
            SourcePath = schemaPath,
            EmitSupportArtifacts = writeRuntime,
            FormatAssertionEnabled = formatAssertionEnabled
        };

        return await GenerateWithTargetAsync(targets, JavaScriptTargetId, schemaPath, outputPath, options);
    }

    private static async Task<int> HandleGenerateJsFromTypeScriptAsync(
        IReadOnlyDictionary<string, ICodeGenerationTarget> targets,
        string schemaPath,
        string outputPath,
        bool writeRuntime,
        bool formatAssertionEnabled,
        string ecmaScriptTarget,
        string tscExecutable)
    {
        Console.WriteLine("Pipeline: TypeScript-first via tsc");
        Console.WriteLine($"ECMAScript target: {ecmaScriptTarget}");

        var options = new TypeScriptCodeGenerationOptions
        {
            SourcePath = schemaPath,
            EmitSupportArtifacts = writeRuntime,
            FormatAssertionEnabled = formatAssertionEnabled
        };

        var request = CreateGenerationRequest(schemaPath, options);
        var capabilityResult = await GetTarget(targets, TypeScriptTargetId).GetCapabilitiesAsync(request);
        if (!capabilityResult.CanGenerate)
        {
            PrintDiagnostics(capabilityResult.Diagnostics);
            return 1;
        }

        var result = await GetTarget(targets, TypeScriptTargetId).GenerateAsync(request);
        if (!result.Success)
        {
            PrintGenerationFailure(result.Diagnostics);
            return 1;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), "jsv-tsgen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        try
        {
            if (!TryGetSinglePrimaryArtifact(result.Artifacts, out var primaryArtifact, out var primaryArtifactError))
            {
                Console.Error.WriteLine($"Generation failed: {primaryArtifactError}");
                return 1;
            }

            var artifactPaths = WriteArtifacts(tempPath, result.Artifacts, announce: false);
            var validatorTsPath = Path.GetFullPath(Path.Combine(tempPath, primaryArtifact.RelativePath));
            var runtimeDeclarationPath = Path.Combine(tempPath, TsRuntime.DeclarationFileName);

            if (!writeRuntime)
            {
                File.WriteAllText(runtimeDeclarationPath, TsRuntime.GetDeclarationSource());
            }

            IReadOnlyList<string> sourcePaths = writeRuntime
                ? artifactPaths
                    .Where(path => string.Equals(Path.GetExtension(path), ".ts", StringComparison.OrdinalIgnoreCase))
                    .ToArray()
                : [validatorTsPath, runtimeDeclarationPath];

            var compilationResult = TypeScriptCompiler.Compile(
                sourcePaths,
                outputPath,
                ecmaScriptTarget,
                tscExecutable);
            if (!compilationResult.Success)
            {
                Console.Error.WriteLine($"TypeScript compilation failed: {compilationResult.Error}");
                if (!string.IsNullOrWhiteSpace(compilationResult.StandardError))
                {
                    Console.Error.WriteLine(compilationResult.StandardError);
                }
                if (!string.IsNullOrWhiteSpace(compilationResult.StandardOutput))
                {
                    Console.Error.WriteLine(compilationResult.StandardOutput);
                }
                return 1;
            }

            var validatorJsPath = Path.Combine(outputPath, Path.ChangeExtension(primaryArtifact.RelativePath, ".js"));
            Console.WriteLine($"Generated: {validatorJsPath}");
            if (writeRuntime)
            {
                Console.WriteLine($"Generated: {Path.Combine(outputPath, "jsv-runtime.js")}");
            }
            return 0;
        }
        finally
        {
            try { Directory.Delete(tempPath, recursive: true); } catch { /* best effort */ }
        }
    }

    private static async Task<int> HandleGenerateTsAsync(
        string[] args,
        IReadOnlyDictionary<string, ICodeGenerationTarget> targets)
    {
        string? schemaPath = null;
        string? outputPath = null;
        var writeRuntime = true;
        var formatAssertionEnabled = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var nextArg = i + 1 < args.Length ? args[i + 1] : null;
            switch (arg)
            {
                case "-s":
                case "--schema":
                    if (!IsOptionValue(nextArg))
                    {
                        Console.Error.WriteLine($"Error: Option {arg} requires a value.");
                        return 1;
                    }
                    schemaPath = nextArg;
                    i++;
                    break;
                case "-o":
                case "--output":
                    if (!IsOptionValue(nextArg))
                    {
                        Console.Error.WriteLine($"Error: Option {arg} requires a value.");
                        return 1;
                    }
                    outputPath = nextArg;
                    i++;
                    break;
                case "--no-runtime":
                    writeRuntime = false;
                    break;
                case "--assert-format":
                    formatAssertionEnabled = true;
                    break;
            }
        }

        if (string.IsNullOrEmpty(schemaPath))
        {
            Console.Error.WriteLine("Error: Schema path is required (-s, --schema)");
            return 1;
        }
        if (string.IsNullOrEmpty(outputPath))
        {
            Console.Error.WriteLine("Error: Output path is required (-o, --output)");
            return 1;
        }
        if (!File.Exists(schemaPath))
        {
            Console.Error.WriteLine($"Error: Schema file not found: {schemaPath}");
            return 1;
        }
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        Console.WriteLine($"Generating TS validator for: {schemaPath}");
        Console.WriteLine($"Output directory: {outputPath}");

        var options = new TypeScriptCodeGenerationOptions
        {
            SourcePath = schemaPath,
            EmitSupportArtifacts = writeRuntime,
            FormatAssertionEnabled = formatAssertionEnabled
        };

        return await GenerateWithTargetAsync(targets, TypeScriptTargetId, schemaPath, outputPath, options);
    }

    /// <summary>
    /// Returns true if the argument is present (i.e., an option's value wasn't
    /// elided from the end of argv). No leading-'-' check — that would reject
    /// legitimate values like <c>-schema.json</c> and diverges from the existing
    /// generate/generate-metaschemas/compile-test-schemas parsers. "--schema -o out"
    /// will bind schemaPath="-o" and fail later with "schema file not found: -o",
    /// which is adequate.
    /// </summary>
    private static bool IsOptionValue(string? arg)
    {
        return !string.IsNullOrEmpty(arg);
    }

    private static IReadOnlyDictionary<string, ICodeGenerationTarget> CreateTargetRegistry()
    {
        ICodeGenerationTarget[] targets =
        [
            new CSharpCodeGenerationTarget(),
            new JavaScriptCodeGenerationTarget(),
            new TypeScriptCodeGenerationTarget()
        ];

        return targets.ToDictionary(target => target.Descriptor.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static ICodeGenerationTarget GetTarget(
        IReadOnlyDictionary<string, ICodeGenerationTarget> targets,
        string targetId)
    {
        if (targets.TryGetValue(targetId, out var target))
        {
            return target;
        }

        throw new InvalidOperationException($"Code generation target '{targetId}' is not registered.");
    }

    private static async Task<int> GenerateWithTargetAsync(
        IReadOnlyDictionary<string, ICodeGenerationTarget> targets,
        string targetId,
        string schemaPath,
        string outputPath,
        CodeGenerationOptions options)
    {
        var target = GetTarget(targets, targetId);
        var request = CreateGenerationRequest(schemaPath, options);
        var capabilityResult = await target.GetCapabilitiesAsync(request);
        if (!capabilityResult.CanGenerate)
        {
            PrintDiagnostics(capabilityResult.Diagnostics);
            return 1;
        }

        var result = await target.GenerateAsync(request);
        if (!result.Success)
        {
            PrintGenerationFailure(result.Diagnostics);
            return 1;
        }

        WriteArtifacts(outputPath, result.Artifacts, announce: true);
        return 0;
    }

    private static CodeGenerationRequest CreateGenerationRequest(
        string schemaPath,
        CodeGenerationOptions options)
    {
        using var schemaDocument = JsonDocument.Parse(File.ReadAllText(schemaPath));
        return new CodeGenerationRequest
        {
            Schema = schemaDocument.RootElement.Clone(),
            Options = options
        };
    }

    private static IReadOnlyList<string> WriteArtifacts(
        string outputDirectory,
        IReadOnlyList<GeneratedArtifact> artifacts,
        bool announce)
    {
        var outputRoot = Path.GetFullPath(outputDirectory);
        var outputRootWithSeparator = EnsureTrailingDirectorySeparator(outputRoot);
        var writtenPaths = new List<string>(artifacts.Count);

        foreach (var artifact in artifacts)
        {
            if (string.IsNullOrWhiteSpace(artifact.RelativePath))
            {
                throw new InvalidOperationException("Generated artifact has no relative path.");
            }

            if (Path.IsPathRooted(artifact.RelativePath))
            {
                throw new InvalidOperationException(
                    $"Generated artifact path must be relative: {artifact.RelativePath}");
            }

            var artifactPath = Path.GetFullPath(Path.Combine(outputRoot, artifact.RelativePath));
            if (!artifactPath.StartsWith(outputRootWithSeparator, GetPathComparison()))
            {
                throw new InvalidOperationException(
                    $"Generated artifact path escapes the output directory: {artifact.RelativePath}");
            }

            var artifactDirectory = Path.GetDirectoryName(artifactPath);
            if (!string.IsNullOrEmpty(artifactDirectory))
            {
                Directory.CreateDirectory(artifactDirectory);
            }

            File.WriteAllText(artifactPath, artifact.Content);
            writtenPaths.Add(artifactPath);
            if (announce)
            {
                Console.WriteLine($"Generated: {artifactPath}");
            }
        }

        return writtenPaths;
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        return Path.EndsInDirectorySeparator(path)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static StringComparison GetPathComparison()
    {
        return OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    private static void PrintGenerationFailure(IReadOnlyList<CodeGenerationDiagnostic> diagnostics)
    {
        Console.Error.WriteLine($"Generation failed: {FormatDiagnostics(diagnostics)}");
    }

    private static void PrintDiagnostics(IReadOnlyList<CodeGenerationDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            Console.Error.WriteLine("Generation failed: target reported that the schema is unsupported.");
            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
        }
    }

    private static string FormatDiagnostics(IReadOnlyList<CodeGenerationDiagnostic> diagnostics)
    {
        return diagnostics.Count == 0
            ? "Unknown error."
            : string.Join("; ", diagnostics.Select(diagnostic => diagnostic.Message));
    }

    private static bool TryGetSinglePrimaryArtifact(
        IReadOnlyList<GeneratedArtifact> artifacts,
        out GeneratedArtifact primaryArtifact,
        out string error)
    {
        var primaryArtifacts = artifacts
            .Where(artifact => artifact.Role == GeneratedArtifactRole.Primary)
            .ToArray();

        if (primaryArtifacts.Length == 1)
        {
            primaryArtifact = primaryArtifacts[0];
            error = string.Empty;
            return true;
        }

        primaryArtifact = null!;
        error = $"Expected exactly one primary generated artifact, but target emitted {primaryArtifacts.Length}.";
        return false;
    }

    private static async Task<int> HandleGenerateMetaschemasAsync(
        string[] args,
        IReadOnlyDictionary<string, ICodeGenerationTarget> targets)
    {
        string? outputPath = null;
        string? libPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var nextArg = i + 1 < args.Length ? args[i + 1] : null;

            switch (arg)
            {
                case "-o":
                case "--output":
                    outputPath = nextArg;
                    i++;
                    break;
                case "-l":
                case "--lib-path":
                    libPath = nextArg;
                    i++;
                    break;
            }
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.Error.WriteLine("Error: Output path is required (-o, --output)");
            return 1;
        }

        if (string.IsNullOrEmpty(libPath))
        {
            Console.Error.WriteLine("Error: Library path is required (-l, --lib-path)");
            return 1;
        }

        if (!Directory.Exists(libPath))
        {
            Console.Error.WriteLine($"Error: Library path not found: {libPath}");
            return 1;
        }

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        Console.WriteLine($"Generating metaschema validators...");
        Console.WriteLine($"Library path: {libPath}");
        Console.WriteLine($"Output directory: {outputPath}");
        Console.WriteLine();

        var target = GetTarget(targets, CSharpTargetId);
        var successCount = 0;
        var failCount = 0;

        foreach (var schema in MetaschemaDefinitions.Schemas)
        {
            var schemaPath = Path.Combine(libPath, schema.DraftFolder, "Data", schema.FileName);

            if (!File.Exists(schemaPath))
            {
                Console.Error.WriteLine($"  SKIP: {schema.FileName} (not found at {schemaPath})");
                failCount++;
                continue;
            }

            var draftOutputPath = Path.Combine(outputPath, schema.DraftFolder);
            if (!Directory.Exists(draftOutputPath))
            {
                Directory.CreateDirectory(draftOutputPath);
            }

            var options = new CSharpCodeGenerationOptions
            {
                SourcePath = schemaPath,
                UseGeneratedRegex = true,
                OutputHints = new CodeGenerationOutputHints
                {
                    NamespaceName = "FormFinch.JsonSchemaValidation.CompiledValidators.Generated",
                    TypeName = $"CompiledValidator_{schema.ClassName}"
                }
            };
            var request = CreateGenerationRequest(schemaPath, options);
            var capabilityResult = await target.GetCapabilitiesAsync(request);
            if (!capabilityResult.CanGenerate)
            {
                Console.Error.WriteLine($"  FAIL: {schema.FileName} - {FormatDiagnostics(capabilityResult.Diagnostics)}");
                failCount++;
                continue;
            }

            var result = await target.GenerateAsync(request);

            if (result.Success)
            {
                if (!TryGetSinglePrimaryArtifact(result.Artifacts, out var primaryArtifact, out var primaryArtifactError))
                {
                    Console.Error.WriteLine($"  FAIL: {schema.FileName} - {primaryArtifactError}");
                    failCount++;
                    continue;
                }

                WriteArtifacts(draftOutputPath, result.Artifacts, announce: false);
                Console.WriteLine($"  OK: {schema.DraftFolder}/{Path.GetFileName(primaryArtifact.RelativePath)}");
                successCount++;
            }
            else
            {
                Console.Error.WriteLine($"  FAIL: {schema.FileName} - {FormatDiagnostics(result.Diagnostics)}");
                failCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Generated {successCount} validators, {failCount} failed.");

        return failCount > 0 ? 1 : 0;
    }

    private static async Task<int> HandleCompileTestSchemasAsync(
        string[] args,
        IReadOnlyDictionary<string, ICodeGenerationTarget> targets)
    {
        string? testSuitePath = null;
        string? outputPath = null;
        var namespaceName = "JsonSchemaValidationBenchmarks.Generated";
        string? draftFilter = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var nextArg = i + 1 < args.Length ? args[i + 1] : null;

            switch (arg)
            {
                case "-t":
                case "--test-suite":
                    testSuitePath = nextArg;
                    i++;
                    break;
                case "-o":
                case "--output":
                    outputPath = nextArg;
                    i++;
                    break;
                case "-n":
                case "--namespace":
                    namespaceName = nextArg ?? namespaceName;
                    i++;
                    break;
                case "-d":
                case "--draft":
                    draftFilter = nextArg;
                    i++;
                    break;
            }
        }

        if (string.IsNullOrEmpty(testSuitePath))
        {
            Console.Error.WriteLine("Error: Test suite path is required (-t, --test-suite)");
            return 1;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.Error.WriteLine("Error: Output path is required (-o, --output)");
            return 1;
        }

        var testsPath = Path.Combine(testSuitePath, "tests");
        if (!Directory.Exists(testsPath))
        {
            Console.Error.WriteLine($"Error: Test suite tests directory not found: {testsPath}");
            return 1;
        }

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        Console.WriteLine("Compiling test suite schemas...");
        Console.WriteLine($"Test suite: {testSuitePath}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine($"Namespace: {namespaceName}");
        if (draftFilter != null)
        {
            Console.WriteLine($"Draft filter: {draftFilter}");
        }
        Console.WriteLine();

        // Determine which draft directories to scan
        var draftDirs = new List<(string DirName, string DraftName)>();
        if (draftFilter == null)
        {
            draftDirs.Add(("draft2020-12", "Draft202012"));
            draftDirs.Add(("draft2019-09", "Draft201909"));
            draftDirs.Add(("draft7", "Draft7"));
            draftDirs.Add(("draft6", "Draft6"));
            draftDirs.Add(("draft4", "Draft4"));
            draftDirs.Add(("draft3", "Draft3"));
        }
        else
        {
            var dir = draftFilter.ToLowerInvariant() switch
            {
                "2020-12" => ("draft2020-12", "Draft202012"),
                "2019-09" => ("draft2019-09", "Draft201909"),
                "7" => ("draft7", "Draft7"),
                "6" => ("draft6", "Draft6"),
                "4" => ("draft4", "Draft4"),
                "3" => ("draft3", "Draft3"),
                _ => (draftFilter, draftFilter.Replace("-", ""))
            };
            draftDirs.Add(dir);
        }

        var target = GetTarget(targets, CSharpTargetId);
        var uniqueSchemas = new Dictionary<string, (JsonElement Schema, string DraftName)>(StringComparer.Ordinal);
        var successCount = 0;
        var failCount = 0;
        var skippedCount = 0;

        // Phase 1: Collect all unique schemas by hash
        Console.WriteLine("Phase 1: Scanning test files for unique schemas...");
        foreach (var (dirName, draftName) in draftDirs)
        {
            var draftTestsPath = Path.Combine(testsPath, dirName);
            if (!Directory.Exists(draftTestsPath))
            {
                Console.WriteLine($"  Skipping {dirName} (directory not found)");
                continue;
            }

            var testFiles = Directory.GetFiles(draftTestsPath, "*.json");
            Console.WriteLine($"  {dirName}: {testFiles.Length} test files");

            foreach (var testFile in testFiles)
            {
                try
                {
                    var json = File.ReadAllText(testFile);
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var testGroup in doc.RootElement.EnumerateArray())
                    {
                        if (!testGroup.TryGetProperty("schema", out var schema))
                        {
                            continue;
                        }

                        var hash = SchemaHasher.ComputeHash(schema);
                        if (!uniqueSchemas.ContainsKey(hash))
                        {
                            uniqueSchemas[hash] = (schema.Clone(), draftName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"    Error reading {Path.GetFileName(testFile)}: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"  Found {uniqueSchemas.Count} unique schemas");
        Console.WriteLine();

        // Phase 2: Generate compiled validators
        Console.WriteLine("Phase 2: Generating compiled validators...");
        var generatedValidators = new List<(string Hash, string ClassName)>();

        foreach (var (hash, (schema, _)) in uniqueSchemas)
        {
            var className = $"CompiledTestSchema_{hash}";

            try
            {
                var request = new CodeGenerationRequest
                {
                    Schema = schema,
                    Options = new CSharpCodeGenerationOptions
                    {
                        SourcePath = $"{className}.json",
                        UseGeneratedRegex = true,
                        OutputHints = new CodeGenerationOutputHints
                        {
                            NamespaceName = namespaceName,
                            TypeName = className
                        }
                    }
                };
                var capabilityResult = await target.GetCapabilitiesAsync(request);
                if (!capabilityResult.CanGenerate)
                {
                    skippedCount++;
                    continue;
                }

                var result = await target.GenerateAsync(request);

                if (result.Success)
                {
                    WriteArtifacts(outputPath, result.Artifacts, announce: false);
                    generatedValidators.Add((hash, className));
                    successCount++;
                }
                else
                {
                    // Skip schemas that can't be compiled (e.g., have external $ref)
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"    Error generating {className}: {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine($"  Generated: {successCount}, Skipped: {skippedCount}, Failed: {failCount}");
        Console.WriteLine();

        // Phase 3: Generate registry class
        Console.WriteLine("Phase 3: Generating registry class...");
        var registryCode = GenerateRegistryClass(namespaceName, generatedValidators);
        var registryPath = Path.Combine(outputPath, "CompiledTestSchemaRegistry.cs");
        File.WriteAllText(registryPath, registryCode);
        Console.WriteLine($"  Generated: {registryPath}");
        Console.WriteLine();

        Console.WriteLine($"Done! Generated {successCount} validators + registry.");
        return failCount > 0 ? 1 : 0;
    }

    private static string GenerateRegistryClass(string namespaceName, List<(string Hash, string ClassName)> validators)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by jsv-codegen compile-test-schemas.");
        sb.AppendLine("// Do not edit this file manually.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using FormFinch.JsonSchemaValidation.Abstractions;");
        sb.AppendLine("using FormFinch.JsonSchemaValidation.CompiledValidators;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Registry of compiled validators for test suite schemas, indexed by content hash.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class CompiledTestSchemaRegistry");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly Dictionary<string, ICompiledValidator> _validators = new(StringComparer.Ordinal)");
        sb.AppendLine("    {");

        foreach (var (hash, className) in validators)
        {
            sb.AppendLine($"        [\"{hash}\"] = new {className}(),");
        }

        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets all compiled validators with their content hashes.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static IReadOnlyDictionary<string, ICompiledValidator> GetAll() => _validators;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Registers all compiled test schema validators in the given registry.");
        sb.AppendLine("    /// Also initializes any registry-aware validators that have external $ref dependencies.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void RegisterAll(ICompiledValidatorRegistry registry)");
        sb.AppendLine("    {");
        sb.AppendLine("        // First pass: register all validators");
        sb.AppendLine("        foreach (var (hash, validator) in _validators)");
        sb.AppendLine("        {");
        sb.AppendLine("            registry.RegisterByHash(hash, validator);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Second pass: initialize registry-aware validators");
        sb.AppendLine("        foreach (var validator in _validators.Values)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (validator is IRegistryAwareCompiledValidator registryAware)");
        sb.AppendLine("            {");
        sb.AppendLine("                registryAware.Initialize(registry);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the number of compiled validators.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static int Count => {validators.Count};");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static int HandleAnalyze(string[] args)
    {
        string? schemaPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var nextArg = i + 1 < args.Length ? args[i + 1] : null;

            switch (arg)
            {
                case "-s":
                case "--schema":
                    schemaPath = nextArg;
                    i++;
                    break;
            }
        }

        if (string.IsNullOrEmpty(schemaPath))
        {
            Console.Error.WriteLine("Error: Schema path is required (-s, --schema)");
            return 1;
        }

        if (!File.Exists(schemaPath))
        {
            Console.Error.WriteLine($"Error: Schema file not found: {schemaPath}");
            return 1;
        }

        Console.WriteLine($"Analyzing: {schemaPath}");

        var analyzer = new SchemaAnalyzer();
        var result = analyzer.Analyze(schemaPath);

        Console.WriteLine();
        Console.WriteLine("Analysis Results:");
        Console.WriteLine($"  Total subschemas: {result.TotalSubschemas}");
        Console.WriteLine($"  Unique subschemas: {result.UniqueSubschemas}");
        Console.WriteLine($"  Fully inlinable: {result.FullyInlinable}");

        if (result.FallbackKeywords.Count > 0)
        {
            Console.WriteLine("  Keywords requiring fallback:");
            foreach (var keyword in result.FallbackKeywords)
            {
                Console.WriteLine($"    - {keyword}");
            }
        }

        return 0;
    }
}
