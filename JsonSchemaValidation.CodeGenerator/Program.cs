using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
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
              generate              Generate compiled validator from schema file
              generate-metaschemas  Generate compiled validators for all metaschemas
              compile-test-schemas  Generate compiled validators for JSON-Schema-Test-Suite schemas
              analyze               Analyze schema for compilation compatibility

            generate options:
              -s, --schema <path>     Input schema file (required)
              -o, --output <path>     Output directory for generated code (required)
              -n, --namespace <name>  Namespace for generated classes (default: JsonSchemaValidation.Generated)
              -c, --class <name>      Class name (defaults to derived from schema $id)

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

        var generator = new SchemaCodeGenerator();
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

        var generator = new SchemaCodeGenerator();
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
                "JsonSchemaValidation.CompiledValidators.Generated",
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

        var generator = new SchemaCodeGenerator();
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
