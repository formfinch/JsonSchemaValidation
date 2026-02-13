namespace FormFinch.JsonSchemaValidation.ManualTests.Scenarios;

internal static class ErrorQualityScenarios
{
    public static void Run(TestRunner runner)
    {
        runner.PrintSection("Error Quality — Messages and Paths");

        RunSimpleErrors(runner);
        RunDeepNesting(runner);
        RunComposition(runner);
        RunConditionals(runner);
        RunReferences(runner);
        RunDependencies(runner);
    }

    private static void RunSimpleErrors(TestRunner runner)
    {
        runner.Run("Wrong type", () =>
        {
            var result = JsonSchemaValidator.Validate(
                """{"type": "string"}""",
                "42");
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors:");
            TestRunner.PrintErrors(result);
        });

        runner.Run("Missing required property", () =>
        {
            var result = JsonSchemaValidator.Validate(
                """{"type": "object", "required": ["name"]}""",
                """{}""");
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors:");
            TestRunner.PrintErrors(result);
        });

        runner.Run("Pattern mismatch", () =>
        {
            var result = JsonSchemaValidator.Validate(
                """{"type": "string", "pattern": "^[A-Z]{3}$"}""",
                "\"abc\"");
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors:");
            TestRunner.PrintErrors(result);
        });
    }

    private static void RunDeepNesting(TestRunner runner)
    {
        runner.Run("Deep nesting — users[0].address.zipCode", () =>
        {
            const string schema = """
                {
                    "type": "object",
                    "properties": {
                        "users": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "address": {
                                        "type": "object",
                                        "properties": {
                                            "zipCode": {
                                                "type": "string",
                                                "pattern": "^[0-9]{5}$"
                                            }
                                        },
                                        "required": ["zipCode"]
                                    }
                                },
                                "required": ["address"]
                            }
                        }
                    }
                }
                """;
            var instance = """{"users": [{"address": {"zipCode": "ABCDE"}}]}""";

            var result = JsonSchemaValidator.Validate(schema, instance);
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (expect path /users/0/address/zipCode):");
            TestRunner.PrintErrors(result);
        });

        runner.Run("Deep nesting — order.items[2].variants[0].sku", () =>
        {
            const string schema = """
                {
                    "type": "object",
                    "properties": {
                        "order": {
                            "type": "object",
                            "properties": {
                                "items": {
                                    "type": "array",
                                    "items": {
                                        "type": "object",
                                        "properties": {
                                            "variants": {
                                                "type": "array",
                                                "items": {
                                                    "type": "object",
                                                    "properties": {
                                                        "sku": { "type": "string", "minLength": 5 }
                                                    },
                                                    "required": ["sku"]
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                """;
            var instance = """{"order": {"items": [{}, {}, {"variants": [{"sku": "AB"}]}]}}""";

            var result = JsonSchemaValidator.Validate(schema, instance);
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (expect path /order/items/2/variants/0/sku):");
            TestRunner.PrintErrors(result);
        });
    }

    private static void RunComposition(TestRunner runner)
    {
        runner.Run("allOf — one branch fails", () =>
        {
            const string schema = """
                {
                    "allOf": [
                        { "type": "object", "properties": { "a": { "type": "string" } }, "required": ["a"] },
                        { "type": "object", "properties": { "b": { "type": "integer" } }, "required": ["b"] },
                        { "type": "object", "properties": { "c": { "type": "boolean" } }, "required": ["c"] }
                    ]
                }
                """;
            var result = JsonSchemaValidator.Validate(schema, """{"a": "ok", "b": 1}""");
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (allOf — missing 'c'):");
            TestRunner.PrintErrors(result);
        });

        runner.Run("anyOf — no branch matches", () =>
        {
            const string schema = """
                {
                    "anyOf": [
                        { "type": "string" },
                        { "type": "integer", "minimum": 10 }
                    ]
                }
                """;
            var result = JsonSchemaValidator.Validate(schema, "5");
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (anyOf — neither branch matches):");
            TestRunner.PrintErrors(result);
        });

        runner.Run("oneOf — zero matches", () =>
        {
            const string schema = """
                {
                    "oneOf": [
                        { "type": "string" },
                        { "type": "integer" }
                    ]
                }
                """;
            var result = JsonSchemaValidator.Validate(schema, "true");
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (oneOf — zero matches):");
            TestRunner.PrintErrors(result);
        });

        runner.Run("oneOf — two matches", () =>
        {
            const string schema = """
                {
                    "oneOf": [
                        { "type": "integer" },
                        { "minimum": 0 }
                    ]
                }
                """;
            var result = JsonSchemaValidator.Validate(schema, "5");
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (oneOf — two matches):");
            TestRunner.PrintErrors(result);
        });

        runner.Run("not — value matches when it shouldn't", () =>
        {
            const string schema = """{ "not": { "type": "string" } }""";
            var result = JsonSchemaValidator.Validate(schema, "\"hello\"");
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (not — matched):");
            TestRunner.PrintErrors(result);
        });
    }

    private static void RunConditionals(TestRunner runner)
    {
        runner.Run("if/then/else — matches if, fails then", () =>
        {
            const string schema = """
                {
                    "$schema": "https://json-schema.org/draft/2020-12/schema",
                    "type": "object",
                    "properties": {
                        "type": { "type": "string" },
                        "value": {}
                    },
                    "if": {
                        "properties": { "type": { "const": "number" } }
                    },
                    "then": {
                        "properties": { "value": { "type": "number" } }
                    },
                    "else": {
                        "properties": { "value": { "type": "string" } }
                    }
                }
                """;
            // type is "number" so `if` matches, but value is a string → fails `then`
            var instance = """{"type": "number", "value": "not-a-number"}""";
            var result = JsonSchemaValidator.Validate(schema, instance);
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (if/then — type=number but value is string):");
            TestRunner.PrintErrors(result);
        });
    }

    private static void RunReferences(TestRunner runner)
    {
        runner.Run("$ref — error through $defs reference", () =>
        {
            const string schema = """
                {
                    "$schema": "https://json-schema.org/draft/2020-12/schema",
                    "$defs": {
                        "address": {
                            "type": "object",
                            "properties": {
                                "street": { "type": "string" },
                                "city": { "type": "string" }
                            },
                            "required": ["street", "city"]
                        }
                    },
                    "type": "object",
                    "properties": {
                        "home": { "$ref": "#/$defs/address" },
                        "work": { "$ref": "#/$defs/address" }
                    }
                }
                """;
            var instance = """{"home": {"street": "123 Main"}, "work": {"street": "456 Oak", "city": "Springfield"}}""";
            var result = JsonSchemaValidator.Validate(schema, instance);
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors ($ref — home missing city):");
            TestRunner.PrintErrors(result);
        });
    }

    private static void RunDependencies(TestRunner runner)
    {
        runner.Run("dependentRequired — dependent property missing", () =>
        {
            const string schema = """
                {
                    "$schema": "https://json-schema.org/draft/2020-12/schema",
                    "type": "object",
                    "properties": {
                        "creditCard": { "type": "string" },
                        "billingAddress": { "type": "string" }
                    },
                    "dependentRequired": {
                        "creditCard": ["billingAddress"]
                    }
                }
                """;
            var instance = """{"creditCard": "4111-1111-1111-1111"}""";
            var result = JsonSchemaValidator.Validate(schema, instance);
            Assert(!result.Valid, "Expected invalid");
            Console.WriteLine("    Errors (dependentRequired — creditCard present but billingAddress missing):");
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
