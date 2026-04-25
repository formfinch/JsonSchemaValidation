// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.TypeScript;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.CodeGenerator;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLowerInvariant();
        var remainingArgs = args.Skip(1).ToArray();

        return command switch
        {
            "generate" => HandleGenerate(remainingArgs),
            "generate-js" => HandleGenerateJs(remainingArgs),
            "generate-ts" => HandleGenerateTs(remainingArgs),
            "generate-metaschemas" => HandleGenerateMetaschemas(remainingArgs),
            "compile-test-schemas" => HandleCompileTestSchemas(remainingArgs),
            "analyze" => HandleAnalyze(remainingArgs),
            "--help" or "-h" or "help" => PrintUsage(),
            _ => PrintUsage()
        };
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

    private static int HandleGenerate(string[] args)
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

        // Use GeneratedRegex for AOT compilation - source generator will provide implementations
        var generator = new CSharpSchemaCodeGenerator { UseGeneratedRegex = true };
        var result = generator.Generate(schemaPath, namespaceName, className);

        if (result.Success)
        {
            var fullOutputPath = Path.Combine(outputPath, result.FileName!);
            File.WriteAllText(fullOutputPath, result.GeneratedCode);
            Console.WriteLine($"Generated: {fullOutputPath}");
            return 0;
        }

        Console.Error.WriteLine($"Generation failed: {result.Error}");
        return 1;
    }

    private static int HandleGenerateJs(string[] args)
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
            return HandleGenerateJsFromTypeScript(
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

        var generator = new JsSchemaCodeGenerator
        {
            FormatAssertionEnabled = formatAssertionEnabled
        };
        var result = generator.Generate(schemaPath);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Generation failed: {result.Error}");
            return 1;
        }

        var validatorPath = Path.Combine(outputPath, result.FileName!);
        File.WriteAllText(validatorPath, result.GeneratedCode!);
        Console.WriteLine($"Generated: {validatorPath}");

        if (writeRuntime)
        {
            var runtimePath = Path.Combine(outputPath, JsRuntime.FileName);
            File.WriteAllText(runtimePath, JsRuntime.GetSource());
            Console.WriteLine($"Generated: {runtimePath}");
        }
        return 0;
    }

    private static int HandleGenerateJsFromTypeScript(
        string schemaPath,
        string outputPath,
        bool writeRuntime,
        bool formatAssertionEnabled,
        string ecmaScriptTarget,
        string tscExecutable)
    {
        Console.WriteLine("Pipeline: TypeScript-first via tsc");
        Console.WriteLine($"ECMAScript target: {ecmaScriptTarget}");

        var generator = new TsSchemaCodeGenerator
        {
            FormatAssertionEnabled = formatAssertionEnabled
        };
        var result = generator.Generate(schemaPath);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Generation failed: {result.Error}");
            return 1;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), "jsv-tsgen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        try
        {
            var validatorTsPath = Path.Combine(tempPath, result.FileName!);
            var runtimeTsPath = Path.Combine(tempPath, TsRuntime.FileName);
            var runtimeDeclarationPath = Path.Combine(tempPath, TsRuntime.DeclarationFileName);
            File.WriteAllText(validatorTsPath, result.GeneratedCode!);
            if (writeRuntime)
            {
                File.WriteAllText(runtimeTsPath, TsRuntime.GetSource());
            }
            else
            {
                File.WriteAllText(runtimeDeclarationPath, TsRuntime.GetDeclarationSource());
            }

            IReadOnlyList<string> sourcePaths = writeRuntime
                ? [validatorTsPath, runtimeTsPath]
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

            var validatorJsPath = Path.Combine(outputPath, Path.ChangeExtension(result.FileName!, ".js"));
            Console.WriteLine($"Generated: {validatorJsPath}");
            if (writeRuntime)
            {
                Console.WriteLine($"Generated: {Path.Combine(outputPath, JsRuntime.FileName)}");
            }
            return 0;
        }
        finally
        {
            try { Directory.Delete(tempPath, recursive: true); } catch { /* best effort */ }
        }
    }

    private static int HandleGenerateTs(string[] args)
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

        var generator = new TsSchemaCodeGenerator
        {
            FormatAssertionEnabled = formatAssertionEnabled
        };
        var result = generator.Generate(schemaPath);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Generation failed: {result.Error}");
            return 1;
        }

        var validatorPath = Path.Combine(outputPath, result.FileName!);
        File.WriteAllText(validatorPath, result.GeneratedCode!);
        Console.WriteLine($"Generated: {validatorPath}");

        if (writeRuntime)
        {
            var runtimePath = Path.Combine(outputPath, TsRuntime.FileName);
            File.WriteAllText(runtimePath, TsRuntime.GetSource());
            Console.WriteLine($"Generated: {runtimePath}");
        }
        return 0;
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

    private static int HandleGenerateMetaschemas(string[] args)
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

        // Use GeneratedRegex for AOT compilation - source generator will provide implementations
        var generator = new CSharpSchemaCodeGenerator { UseGeneratedRegex = true };
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

            var result = generator.Generate(
                schemaPath,
                "FormFinch.JsonSchemaValidation.CompiledValidators.Generated",
                $"CompiledValidator_{schema.ClassName}");

            if (result.Success)
            {
                var fullOutputPath = Path.Combine(draftOutputPath, result.FileName!);
                File.WriteAllText(fullOutputPath, result.GeneratedCode);
                Console.WriteLine($"  OK: {schema.DraftFolder}/{result.FileName}");
                successCount++;
            }
            else
            {
                Console.Error.WriteLine($"  FAIL: {schema.FileName} - {result.Error}");
                failCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Generated {successCount} validators, {failCount} failed.");

        return failCount > 0 ? 1 : 0;
    }

    private static int HandleCompileTestSchemas(string[] args)
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

        // Use GeneratedRegex for AOT compilation - source generator will provide implementations
        var generator = new CSharpSchemaCodeGenerator { UseGeneratedRegex = true };
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

        foreach (var (hash, (schema, draftName)) in uniqueSchemas)
        {
            var className = $"CompiledTestSchema_{hash}";

            try
            {
                // Write schema to temp file for generator
                var tempSchemaPath = Path.Combine(Path.GetTempPath(), $"schema_{hash}.json");
                File.WriteAllText(tempSchemaPath, schema.GetRawText());

                var result = generator.Generate(tempSchemaPath, namespaceName, className);

                File.Delete(tempSchemaPath);

                if (result.Success)
                {
                    var fullOutputPath = Path.Combine(outputPath, $"{className}.cs");
                    File.WriteAllText(fullOutputPath, result.GeneratedCode);
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
