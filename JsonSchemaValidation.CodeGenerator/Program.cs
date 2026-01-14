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
              generate    Generate compiled validator from schema file
              analyze     Analyze schema for compilation compatibility

            generate options:
              -s, --schema <path>     Input schema file (required)
              -o, --output <path>     Output directory for generated code (required)
              -n, --namespace <name>  Namespace for generated classes (default: JsonSchemaValidation.Generated)
              -c, --class <name>      Class name (defaults to derived from schema $id)

            analyze options:
              -s, --schema <path>     Input schema file to analyze (required)

            Examples:
              jsv-codegen generate -s schema.json -o ./Generated/
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
