using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Validation.Output;

namespace FormFinch.JsonSchemaValidation.ManualTests.Scenarios;

internal static class FormatValidationScenarios
{
    private static readonly (string Format, string ValidValue, string InvalidValue)[] Formats =
    [
        ("email", "\"user@example.com\"", "\"not-an-email\""),
        ("date-time", "\"2024-01-15T10:30:00Z\"", "\"not-a-date\""),
        ("uri", "\"https://example.com/path\"", "\"not a uri\""),
        ("ipv4", "\"192.168.1.1\"", "\"999.999.999.999\""),
        ("uuid", "\"550e8400-e29b-41d4-a716-446655440000\"", "\"not-a-uuid\""),
    ];

    public static void Run(TestRunner runner)
    {
        runner.PrintSection("Format Validation — Annotation vs Assertion");

        RunWithoutAssertion(runner);
        RunWithAssertion(runner);
    }

    private static void RunWithoutAssertion(TestRunner runner)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("  [Without format assertion — annotation-only (2020-12 default)]");
        Console.ResetColor();

        foreach (var (format, _, invalidValue) in Formats)
        {
            runner.Run($"format '{format}' annotation-only — invalid value passes", () =>
            {
                var schema = $$"""
                    {
                        "$schema": "https://json-schema.org/draft/2020-12/schema",
                        "type": "string",
                        "format": "{{format}}"
                    }
                    """;
                var isValid = JsonSchemaValidator.IsValid(schema, invalidValue);
                Assert(isValid, $"Expected valid (annotation-only) but got invalid for format '{format}'");
            });
        }
    }

    private static void RunWithAssertion(TestRunner runner)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("  [With format assertion enabled]");
        Console.ResetColor();

        var options = new SchemaValidationOptions();
        options.Draft202012.FormatAssertionEnabled = true;

        foreach (var (format, validValue, invalidValue) in Formats)
        {
            runner.Run($"format '{format}' assertion — valid value passes", () =>
            {
                var schema = $$"""
                    {
                        "$schema": "https://json-schema.org/draft/2020-12/schema",
                        "type": "string",
                        "format": "{{format}}"
                    }
                    """;
                var result = JsonSchemaValidator.Validate(schema, validValue, options);
                Assert(result.Valid, $"Expected valid for format '{format}' with value {validValue}");
            });

            runner.Run($"format '{format}' assertion — invalid value fails", () =>
            {
                var schema = $$"""
                    {
                        "$schema": "https://json-schema.org/draft/2020-12/schema",
                        "type": "string",
                        "format": "{{format}}"
                    }
                    """;
                var result = JsonSchemaValidator.Validate(schema, invalidValue, options);
                Assert(!result.Valid, $"Expected invalid for format '{format}' with value {invalidValue}");
                Console.WriteLine($"    Error for '{format}':");
                TestRunner.PrintErrors(result);
            });
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
