// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Diagnostics;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Exceptions;

namespace FormFinch.JsonSchemaValidation.Tests.Stress.Fuzzing;

/// <summary>
/// Tests with known pathological regex patterns (ReDoS - Regular Expression Denial of Service)
/// to verify that timeout protection works correctly.
/// </summary>
/// <remarks>
/// These tests are in a separate project and not run by default.
/// Run with: dotnet test JsonSchemaValidationTests.Stress
/// Timing assertions are intentionally loose to accommodate CI environments.
/// </remarks>
[Trait("Category", "Fuzzing")]
[Trait("Category", "ReDoS")]
public class ReDoSTests
{
    // The library uses a 5-second timeout for regex operations
    // Use generous bounds to avoid flaky failures on slow CI hosts
    private static readonly TimeSpan ExpectedMaxDuration = TimeSpan.FromSeconds(30);

    #region Known Pathological Patterns

    [Fact]
    public void PathologicalPattern_NestedQuantifiers_TimesOutOrCompletes()
    {
        // Pattern: (a+)+ - classic ReDoS pattern
        // Pathological input: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaab"
        var schema = """{"pattern": "(a+)+$"}""";
        var evilInput = "\"" + new string('a', 30) + "b\"";

        var sw = Stopwatch.StartNew();
        try
        {
            var result = JsonSchemaValidator.Validate(schema, evilInput);
            sw.Stop();

            // If it completed, it should be within acceptable time
            Assert.True(sw.Elapsed < ExpectedMaxDuration,
                $"Pattern took {sw.Elapsed.TotalSeconds:F2}s which exceeds {ExpectedMaxDuration.TotalSeconds}s");
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout is the expected protective behavior
            sw.Stop();
            Assert.True(sw.Elapsed < ExpectedMaxDuration, "Timeout took too long");
        }
    }

    [Fact]
    public void PathologicalPattern_AlternationWithOverlap_TimesOutOrCompletes()
    {
        // Pattern: (a|aa)+ - exponential backtracking
        var schema = """{"pattern": "(a|aa)+$"}""";
        var evilInput = "\"" + new string('a', 25) + "b\"";

        var sw = Stopwatch.StartNew();
        try
        {
            var result = JsonSchemaValidator.Validate(schema, evilInput);
            sw.Stop();

            Assert.True(sw.Elapsed < ExpectedMaxDuration,
                $"Pattern took {sw.Elapsed.TotalSeconds:F2}s");
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout is the expected protective behavior - no timing assertion needed
        }
    }

    [Fact]
    public void PathologicalPattern_RepeatedGroupWithBackref_TimesOutOrCompletes()
    {
        // Pattern: (.*a){x} - polynomial blowup
        var schema = """{"pattern": "(.*a){10}"}""";
        var evilInput = "\"" + new string('a', 50) + "b\"";

        var sw = Stopwatch.StartNew();
        try
        {
            var result = JsonSchemaValidator.Validate(schema, evilInput);
            sw.Stop();

            Assert.True(sw.Elapsed < ExpectedMaxDuration);
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout is the expected protective behavior - no timing assertion needed
        }
    }

    [Fact]
    public void PathologicalPattern_NestedAlternation_TimesOutOrCompletes()
    {
        // Pattern: (a|a?)+
        var schema = """{"pattern": "(a|a?)+$"}""";
        var evilInput = "\"" + new string('a', 25) + "b\"";

        var sw = Stopwatch.StartNew();
        try
        {
            var result = JsonSchemaValidator.Validate(schema, evilInput);
            sw.Stop();

            Assert.True(sw.Elapsed < ExpectedMaxDuration);
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout is the expected protective behavior - no timing assertion needed
        }
    }

    [Fact]
    public void PathologicalPattern_EmailLikePattern_TimesOutOrCompletes()
    {
        // Real-world vulnerable email pattern
        var schema = """{"pattern": "^([a-zA-Z0-9])(([-.]|[_]+)?([a-zA-Z0-9]+))*(@){1}[a-z0-9]+[.]{1}(([a-z]{2,3})|([a-z]{2,3}[.]{1}[a-z]{2,3}))$"}""";
        var evilInput = "\"" + new string('a', 30) + "\"";

        var sw = Stopwatch.StartNew();
        try
        {
            var result = JsonSchemaValidator.Validate(schema, evilInput);
            sw.Stop();

            Assert.True(sw.Elapsed < ExpectedMaxDuration);
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout is the expected protective behavior - no timing assertion needed
        }
    }

    #endregion

    #region Timeout Protection Verification

    [Fact]
    public void TimeoutProtection_WorksAcrossMultipleValidations()
    {
        // Verify timeout protection works consistently
        var schema = """{"pattern": "(a+)+$"}""";
        var evilInput = "\"" + new string('a', 28) + "b\"";

        var timeouts = 0;
        var completions = 0;

        for (int i = 0; i < 3; i++)
        {
            try
            {
                var result = JsonSchemaValidator.Validate(schema, evilInput);
                completions++;
            }
            catch (RegexMatchTimeoutException)
            {
                timeouts++;
            }
        }

        // Either all timeout or all complete (consistent behavior)
        Assert.True(timeouts == 3 || completions == 3,
            $"Inconsistent behavior: {timeouts} timeouts, {completions} completions");
    }

    [Fact]
    public void NormalPattern_CompletesQuickly()
    {
        // Verify non-pathological patterns work normally
        var schema = """{"pattern": "^[a-z]+$"}""";
        var normalInput = "\"" + new string('a', 10000) + "\"";

        var sw = Stopwatch.StartNew();
        var result = JsonSchemaValidator.Validate(schema, normalInput);
        sw.Stop();

        Assert.True(result.Valid);
        // No strict timing assertion - just verify it completes without timeout
    }

    [Fact]
    public void NonMatchingInput_CompletesQuickly()
    {
        // Even pathological patterns should be fast when input doesn't match early characters
        var schema = """{"pattern": "(a+)+$"}""";
        var nonMatchingInput = "\"xyz\"";

        var sw = Stopwatch.StartNew();
        var result = JsonSchemaValidator.Validate(schema, nonMatchingInput);
        sw.Stop();

        Assert.False(result.Valid);
        // No strict timing assertion - just verify it completes without timeout
    }

    #endregion

    #region Pattern Caching Tests

    [Fact]
    public void SamePattern_UsesCache_ConsistentBehavior()
    {
        var schema = """{"pattern": "^test.*$"}""";

        // First validation
        var result1 = JsonSchemaValidator.Validate(schema, "\"test123\"");

        // Second validation with same pattern (should use cache)
        var result2 = JsonSchemaValidator.Validate(schema, "\"test456\"");

        // Both should give consistent results
        Assert.True(result1.Valid);
        Assert.True(result2.Valid);
    }

    [Fact]
    public void DifferentPatterns_BothWork()
    {
        var schema1 = """{"pattern": "^[0-9]+$"}""";
        var schema2 = """{"pattern": "^[a-z]+$"}""";

        var result1 = JsonSchemaValidator.Validate(schema1, "\"123\"");
        var result2 = JsonSchemaValidator.Validate(schema2, "\"abc\"");
        var result3 = JsonSchemaValidator.Validate(schema1, "\"abc\"");
        var result4 = JsonSchemaValidator.Validate(schema2, "\"123\"");

        Assert.True(result1.Valid);
        Assert.True(result2.Valid);
        Assert.False(result3.Valid);
        Assert.False(result4.Valid);
    }

    #endregion

    #region Edge Case Patterns

    [Theory]
    [InlineData("^$")]              // Empty string only
    [InlineData("^.{0}$")]          // Zero-width match
    [InlineData("(?:)")]            // Empty non-capturing group
    [InlineData("^(?=a)a")]         // Lookahead
    [InlineData("a{0,0}")]          // Zero repetitions
    public void EdgeCasePatterns_HandleCorrectly(string pattern)
    {
        var schema = $$"""{"pattern": "{{pattern}}"}""";

        // Should not throw
        var result = JsonSchemaValidator.Validate(schema, "\"test\"");
        Assert.NotNull(result);
    }

    [Fact]
    public void VeryLongPattern_HandlesCorrectly()
    {
        // Create a long but safe pattern
        var pattern = string.Join("|", Enumerable.Range(0, 100).Select(i => $"option{i}"));
        var schema = $$"""{"pattern": "^({{pattern}})$"}""";

        var result = JsonSchemaValidator.Validate(schema, "\"option50\"");
        Assert.True(result.Valid);
    }

    [Fact]
    public void UnicodePattern_HandlesCorrectly()
    {
        // Unicode patterns should work
        var schema = """{"pattern": "^[\\p{L}]+$"}""";

        var result1 = JsonSchemaValidator.Validate(schema, "\"hello\"");
        var result2 = JsonSchemaValidator.Validate(schema, "\"123\"");

        Assert.True(result1.Valid);
        Assert.False(result2.Valid);
    }

    #endregion

    #region PatternProperties ReDoS

    [Fact]
    public void PathologicalPatternProperties_TimesOutOrCompletes()
    {
        // patternProperties also uses regex
        var schema = """
            {
                "type": "object",
                "patternProperties": {
                    "(a+)+$": {"type": "string"}
                }
            }
            """;

        var evilPropertyName = new string('a', 25) + "b";
        var instance = $$"""{"{{evilPropertyName}}": "value"}""";

        var sw = Stopwatch.StartNew();
        try
        {
            var result = JsonSchemaValidator.Validate(schema, instance);
            sw.Stop();

            Assert.True(sw.Elapsed < ExpectedMaxDuration);
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout is the expected protective behavior - no timing assertion needed
        }
    }

    #endregion

    #region Invalid Patterns

    [Fact]
    public void InvalidPattern_ThrowsAppropriateException()
    {
        // Unbalanced parenthesis
        var schema = """{"pattern": "(unclosed"}""";

        Assert.ThrowsAny<Exception>(() => JsonSchemaValidator.Validate(schema, "\"test\""));
    }

    [Fact]
    public void EmptyStringPattern_ThrowsInvalidSchemaException()
    {
        var schema = """{"pattern": ""}""";

        Assert.Throws<InvalidSchemaException>(() => JsonSchemaValidator.Validate(schema, "\"test\""));
    }

    #endregion
}
