using JsonSchemaValidation.CodeGenerator.CodeGenerator;

namespace JsonSchemaValidation.CodeGenerator;

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
              analyze               Analyze schema for compilation compatibility

            generate options:
              -s, --schema <path>     Input schema file (required)
              -o, --output <path>     Output directory for generated code (required)
              -n, --namespace <name>  Namespace for generated classes (default: JsonSchemaValidation.Generated)
              -c, --class <name>      Class name (defaults to derived from schema $id)

            generate-metaschemas options:
              -o, --output <path>     Output directory for generated code (required)
              -l, --lib-path <path>   Path to JsonSchemaValidation project (required)

            analyze options:
              -s, --schema <path>     Input schema file to analyze (required)

            Examples:
              jsv-codegen generate -s schema.json -o ./Generated/
              jsv-codegen generate-metaschemas -o ./Generated/ -l ../JsonSchemaValidation
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
