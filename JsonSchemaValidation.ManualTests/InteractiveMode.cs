namespace FormFinch.JsonSchemaValidation.ManualTests;

internal static class InteractiveMode
{
    public static void Run()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("JSON Schema Validator — Interactive Mode");
        Console.WriteLine(new string('=', 45));
        Console.ResetColor();
        Console.WriteLine();

        while (true)
        {
            var schemaJson = ReadFileContent("Schema file path");
            if (schemaJson == null)
            {
                break;
            }

            var instanceJson = ReadFileContent("Data file path");
            if (instanceJson == null)
            {
                break;
            }

            var mode = ReadOutputMode();
            if (mode == null)
            {
                break;
            }

            Console.WriteLine();
            try
            {
                if (mode == "bool")
                {
                    var isValid = JsonSchemaValidator.IsValid(schemaJson, instanceJson);
                    Console.ForegroundColor = isValid ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"Valid: {isValid}");
                    Console.ResetColor();
                }
                else
                {
                    var format = mode == "detailed" ? OutputFormat.Detailed : OutputFormat.Basic;

                    var result = JsonSchemaValidator.Validate(schemaJson, instanceJson, format);
                    Console.ForegroundColor = result.Valid ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"Valid: {result.Valid}");
                    Console.ResetColor();

                    if (!result.Valid)
                    {
                        Console.WriteLine(mode == "detailed" ? "Errors (hierarchical):" : "Errors:");
                        TestRunner.PrintErrors(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.Write("Validate another? (y/n): ");
            var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (answer != "y")
            {
                break;
            }

            Console.WriteLine();
        }
    }

    private static string? ReadFileContent(string prompt)
    {
        Console.Write($"{prompt}: ");
        var path = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        // Remove surrounding quotes if present
        if (path.Length >= 2 && path[0] == '"' && path[^1] == '"')
        {
            path = path[1..^1];
        }

        if (!File.Exists(path))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"File not found: {path}");
            Console.ResetColor();
            return null;
        }

        return File.ReadAllText(path);
    }

    private static string? ReadOutputMode()
    {
        Console.Write("Output mode — bool / errors / detailed (b/e/d): ");
        var mode = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(mode))
        {
            return null;
        }

        return mode switch
        {
            "b" or "bool" => "bool",
            "d" or "detailed" => "detailed",
            "e" or "errors" => "errors",
            _ => "errors",
        };
    }
}
