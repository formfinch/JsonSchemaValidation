// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

/// <summary>
/// Eager-format tests. These verify the compiled path's "supported formats are
/// validated" behavior, matching the C# compiled path. Suite's annotation-only
/// expectation is explicitly out of scope (excluded from the suite runner).
/// </summary>
public class JsFormatTests
{
    private readonly JsValidatorHarness _harness = new();

    private void Expect(string schema, (string data, bool expected)[] cases)
    {
        var result = _harness.Evaluate(schema, cases.Select(c => c.data));
        Assert.True(result.Success, result.Error);
        for (var i = 0; i < cases.Length; i++)
        {
            Assert.True(
                result.Verdicts[i] == cases[i].expected,
                $"Data {cases[i].data}: expected {cases[i].expected}, got {result.Verdicts[i]}.\n" +
                $"Emitted:\n{result.GeneratedSource}");
        }
    }

    [Fact]
    public void Date_RejectsInvalidDate()
    {
        Expect(
            """{ "format": "date" }""",
            [
                ("\"2023-01-15\"", true),
                ("\"2023-13-01\"", false),
                ("\"not-a-date\"", false),
                ("42", true),
            ]);
    }

    [Fact]
    public void DateTime_Rfc3339()
    {
        Expect(
            """{ "format": "date-time" }""",
            [
                ("\"2023-01-15T12:00:00Z\"", true),
                ("\"2023-01-15T12:00:00+02:00\"", true),
                ("\"2023-01-15 12:00:00Z\"", false),
                ("\"garbage\"", false),
            ]);
    }

    [Fact]
    public void Email_BasicShapes()
    {
        Expect(
            """{ "format": "email" }""",
            [
                ("\"a@b.co\"", true),
                ("\"user.name+tag@example.com\"", true),
                ("\"no-at-sign\"", false),
                ("\"trailing@\"", false),
            ]);
    }

    [Fact]
    public void Uuid_Rfc4122()
    {
        Expect(
            """{ "format": "uuid" }""",
            [
                ("\"550e8400-e29b-41d4-a716-446655440000\"", true),
                ("\"not-a-uuid\"", false),
            ]);
    }

    [Fact]
    public void Ipv4_RejectsOutOfRange()
    {
        Expect(
            """{ "format": "ipv4" }""",
            [
                ("\"192.168.1.1\"", true),
                ("\"256.0.0.0\"", false),
                ("\"1.2.3\"", false),
            ]);
    }

    [Fact]
    public void Regex_RejectsInvalidPattern()
    {
        Expect(
            """{ "format": "regex" }""",
            [
                ("\"^[a-z]+$\"", true),
                ("\"[unclosed\"", false),
            ]);
    }

    [Fact]
    public void Unsupported_Format_IsAnnotationOnly_PerDraft()
    {
        // "duration" exists in 2020-12 but not in Draft 4. When schema targets
        // Draft 4, duration is annotation-only — our emitter skips validation.
        Expect(
            """{ "$schema": "http://json-schema.org/draft-04/schema#", "format": "duration" }""",
            [
                ("\"bogus\"", true),       // annotation-only, no rejection
                ("\"P1Y\"", true),
            ]);
    }
}
