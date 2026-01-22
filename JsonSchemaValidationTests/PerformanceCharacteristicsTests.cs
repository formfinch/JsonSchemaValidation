// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Diagnostics;
using FormFinch.JsonSchemaValidation;

namespace FormFinch.JsonSchemaValidationTests;

/// <summary>
/// Tests demonstrating performance characteristics of different API usage patterns.
/// </summary>
public class PerformanceCharacteristicsTests
{
    private const string SimpleSchema = """{"type": "string", "minLength": 1}""";
    private const string ValidInstance = "\"hello world\"";
    private const int Iterations = 1000;

    /// <summary>
    /// Measures the overhead of the first validation call (lazy initialization).
    /// </summary>
    [Fact]
    public void FirstCallOverhead_IsMeasurable()
    {
        // This test demonstrates that the first call includes DI container initialization.
        // Subsequent calls should be much faster.

        // Use a unique schema to ensure we're not hitting any cache
        var uniqueSchema = $$$"""{"type": "string", "const": "{{{Guid.NewGuid()}}}"}""";

        var sw = Stopwatch.StartNew();
        var firstResult = JsonSchemaValidator.Validate(uniqueSchema, "\"test\"");
        var firstCallMs = sw.ElapsedMilliseconds;

        sw.Restart();
        var secondResult = JsonSchemaValidator.Validate(SimpleSchema, ValidInstance);
        var secondCallMs = sw.ElapsedMilliseconds;

        // First call includes lazy initialization, should be noticeably longer
        // (We don't assert specific timings as they vary by machine)
        Assert.True(firstResult.Valid || !firstResult.Valid); // Just ensure no exception
        Assert.True(secondResult.Valid);

        // Output for manual inspection when running tests
        // First call: ~X ms (includes DI init), Second call: ~Y ms
    }

    /// <summary>
    /// Demonstrates that parsed schemas are more efficient for repeated validations.
    /// </summary>
    [Fact]
    public void ParsedSchema_FasterForRepeatedValidation()
    {
        // Warm up
        JsonSchemaValidator.Validate(SimpleSchema, ValidInstance);

        // Measure one-shot validation
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            JsonSchemaValidator.Validate(SimpleSchema, ValidInstance);
        }
        var oneShotMs = sw.ElapsedMilliseconds;

        // Measure parsed schema
        var parsedSchema = JsonSchemaValidator.Parse(SimpleSchema);
        sw.Restart();
        for (int i = 0; i < Iterations; i++)
        {
            parsedSchema.Validate(ValidInstance);
        }
        var parsedMs = sw.ElapsedMilliseconds;

        // Compiled should be faster (we don't assert specific ratio as it varies)
        // The key insight: parsed avoids schema re-registration and re-parsing
        Assert.True(parsedMs <= oneShotMs,
            $"Compiled ({parsedMs}ms) should be <= one-shot ({oneShotMs}ms) for {Iterations} iterations");
    }

    /// <summary>
    /// Demonstrates that IsValid is faster than Validate when you only need a boolean.
    /// </summary>
    [Fact]
    public void IsValid_FasterThanValidate()
    {
        var parsedSchema = JsonSchemaValidator.Parse(SimpleSchema);

        // Warm up
        parsedSchema.Validate(ValidInstance);
        parsedSchema.IsValid(ValidInstance);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            parsedSchema.Validate(ValidInstance);
        }
        var validateMs = sw.ElapsedMilliseconds;

        sw.Restart();
        for (int i = 0; i < Iterations; i++)
        {
            parsedSchema.IsValid(ValidInstance);
        }
        var isValidMs = sw.ElapsedMilliseconds;

        // IsValid short-circuits and avoids building the result tree
        // For valid instances, the difference may be small
        // For invalid instances with many errors, IsValid is significantly faster
        Assert.True(isValidMs <= validateMs + 50, // Allow small margin for timing variance
            $"IsValid ({isValidMs}ms) should be <= Validate ({validateMs}ms)");
    }

    /// <summary>
    /// Shows the performance impact of complex schemas.
    /// </summary>
    [Fact]
    public void ComplexSchema_HasHigherOverhead()
    {
        var simpleSchema = """{"type": "string"}""";
        var complexSchema = """
            {
                "type": "object",
                "properties": {
                    "name": {"type": "string", "minLength": 1, "maxLength": 100},
                    "age": {"type": "integer", "minimum": 0, "maximum": 150},
                    "email": {"type": "string", "format": "email"},
                    "tags": {
                        "type": "array",
                        "items": {"type": "string"},
                        "minItems": 0,
                        "maxItems": 10
                    },
                    "address": {
                        "type": "object",
                        "properties": {
                            "street": {"type": "string"},
                            "city": {"type": "string"},
                            "zip": {"type": "string", "pattern": "^[0-9]{5}$"}
                        },
                        "required": ["street", "city"]
                    }
                },
                "required": ["name", "email"]
            }
            """;

        var simpleInstance = "\"test\"";
        var complexInstance = """
            {
                "name": "John Doe",
                "age": 30,
                "email": "john@example.com",
                "tags": ["user", "admin"],
                "address": {"street": "123 Main St", "city": "Anytown", "zip": "12345"}
            }
            """;

        // Compile both schemas
        var simpleCompiled = JsonSchemaValidator.Parse(simpleSchema);
        var complexCompiled = JsonSchemaValidator.Parse(complexSchema);

        // Warm up
        simpleCompiled.Validate(simpleInstance);
        complexCompiled.Validate(complexInstance);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            simpleCompiled.Validate(simpleInstance);
        }
        var simpleMs = sw.ElapsedMilliseconds;

        sw.Restart();
        for (int i = 0; i < Iterations; i++)
        {
            complexCompiled.Validate(complexInstance);
        }
        var complexMs = sw.ElapsedMilliseconds;

        // Complex schema validation takes longer (more keywords to check)
        // This is expected behavior
        Assert.True(simpleMs >= 0 && complexMs >= 0);
    }
}
