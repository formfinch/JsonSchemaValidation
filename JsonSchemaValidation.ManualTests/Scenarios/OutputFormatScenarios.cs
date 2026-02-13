namespace FormFinch.JsonSchemaValidation.ManualTests.Scenarios;

internal static class OutputFormatScenarios
{
    private const string Schema = """
        {
            "type": "object",
            "properties": {
                "name": { "type": "string", "minLength": 2 },
                "email": { "type": "string", "format": "email" },
                "age": { "type": "integer", "minimum": 0, "maximum": 150 }
            },
            "required": ["name", "email", "age"]
        }
        """;

    // All properties fail: name too short, email wrong type, age out of range
    private const string InvalidInstance = """{"name": "A", "email": 42, "age": 200}""";

    public static void Run(TestRunner runner)
    {
        runner.PrintSection("Output Formats — Flag, Basic, Detailed");

        runner.Run("Flag format — boolean only", () =>
        {
            var result = JsonSchemaValidator.Validate(Schema, InvalidInstance, OutputFormat.Flag);
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine($"    Valid: {result.Valid}");
            Console.WriteLine($"    Errors: {(result.Errors == null ? "none (flag mode)" : result.Errors.Count.ToString())}");
        });

        runner.Run("Basic format — flat error list", () =>
        {
            var result = JsonSchemaValidator.Validate(Schema, InvalidInstance, OutputFormat.Basic);
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (flat list):");
            TestRunner.PrintErrors(result);
        });

        runner.Run("Detailed format — hierarchical error tree", () =>
        {
            var result = JsonSchemaValidator.Validate(Schema, InvalidInstance, OutputFormat.Detailed);
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (hierarchical):");
            TestRunner.PrintErrors(result);
        });
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
