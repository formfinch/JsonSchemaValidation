namespace FormFinch.JsonSchemaValidation.ManualTests.Scenarios;

internal static class MultiDraftScenarios
{
    public static void Run(TestRunner runner)
    {
        runner.PrintSection("Multi-Draft — Draft 4, 7, 2020-12");

        RunDraft4(runner);
        RunDraft7(runner);
        RunDraft202012(runner);
    }

    private static void RunDraft4(TestRunner runner)
    {
        const string schema = """
            {
                "$schema": "http://json-schema.org/draft-04/schema#",
                "type": "object",
                "properties": {
                    "id": { "type": "integer" }
                },
                "required": ["id"]
            }
            """;

        runner.Run("Draft 4 — valid", () =>
        {
            Assert(JsonSchemaValidator.IsValid(schema, """{"id": 1}"""), "Expected valid");
        });

        runner.Run("Draft 4 — invalid (missing required)", () =>
        {
            Assert(!JsonSchemaValidator.IsValid(schema, """{}"""), "Expected invalid");
        });
    }

    private static void RunDraft7(TestRunner runner)
    {
        const string schema = """
            {
                "$schema": "http://json-schema.org/draft-07/schema#",
                "type": "object",
                "properties": {
                    "status": { "type": "string", "enum": ["active", "inactive"] }
                },
                "required": ["status"],
                "if": { "properties": { "status": { "const": "active" } } },
                "then": { "required": ["activeSince"] }
            }
            """;

        runner.Run("Draft 7 — valid (inactive, no extra required)", () =>
        {
            Assert(JsonSchemaValidator.IsValid(schema, """{"status": "inactive"}"""), "Expected valid");
        });

        runner.Run("Draft 7 — invalid (active but missing activeSince)", () =>
        {
            Assert(!JsonSchemaValidator.IsValid(schema, """{"status": "active"}"""), "Expected invalid");
        });
    }

    private static void RunDraft202012(TestRunner runner)
    {
        const string schema = """
            {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "type": "object",
                "properties": {
                    "tags": {
                        "type": "array",
                        "prefixItems": [
                            { "type": "string" },
                            { "type": "string" }
                        ],
                        "items": false
                    }
                },
                "required": ["tags"]
            }
            """;

        runner.Run("Draft 2020-12 — valid (prefixItems match)", () =>
        {
            Assert(JsonSchemaValidator.IsValid(schema, """{"tags": ["a", "b"]}"""), "Expected valid");
        });

        runner.Run("Draft 2020-12 — invalid (extra item beyond prefixItems)", () =>
        {
            Assert(!JsonSchemaValidator.IsValid(schema, """{"tags": ["a", "b", "c"]}"""), "Expected invalid");
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
