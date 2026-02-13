namespace FormFinch.JsonSchemaValidation.ManualTests.Scenarios;

internal static class StaticApiScenarios
{
    private const string Schema = """
        {
            "type": "object",
            "properties": {
                "name": { "type": "string", "minLength": 1 },
                "age": { "type": "integer", "minimum": 0 }
            },
            "required": ["name", "age"]
        }
        """;

    private const string ValidInstance = """{"name": "Alice", "age": 30}""";
    private const string InvalidInstance = """{"name": "", "age": -1}""";

    public static void Run(TestRunner runner)
    {
        runner.PrintSection("Static API — Validate, IsValid, Parse");

        runner.Run("Validate — valid instance", () =>
        {
            var result = JsonSchemaValidator.Validate(Schema, ValidInstance);
            Assert(result.Valid, "Expected valid");
        });

        runner.Run("Validate — invalid instance", () =>
        {
            var result = JsonSchemaValidator.Validate(Schema, InvalidInstance);
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors:");
            TestRunner.PrintErrors(result);
        });

        runner.Run("IsValid — valid instance", () =>
        {
            var isValid = JsonSchemaValidator.IsValid(Schema, ValidInstance);
            Assert(isValid, "Expected true");
        });

        runner.Run("IsValid — invalid instance", () =>
        {
            var isValid = JsonSchemaValidator.IsValid(Schema, InvalidInstance);
            Assert(!isValid, "Expected false");
        });

        runner.Run("Parse + reuse — validate multiple instances", () =>
        {
            var schema = JsonSchemaValidator.Parse(Schema);
            var result1 = schema.Validate(ValidInstance);
            Assert(result1.Valid, "Expected valid for first instance");

            var result2 = schema.Validate(InvalidInstance);
            Assert(!result2.Valid, "Expected invalid for second instance");

            Assert(schema.IsValid(ValidInstance), "Expected IsValid true");
            Assert(!schema.IsValid(InvalidInstance), "Expected IsValid false");
        });

        runner.Run("Custom options — format assertion enabled", () =>
        {
            var emailSchema = """
                {
                    "$schema": "https://json-schema.org/draft/2020-12/schema",
                    "type": "string",
                    "format": "email"
                }
                """;

            var options = new SchemaValidationOptions();
            options.Draft202012.FormatAssertionEnabled = true;

            var result = JsonSchemaValidator.Validate(emailSchema, "\"not-an-email\"", options);
            Assert(!result.Valid, "Expected invalid with format assertion");
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
