// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

/// <summary>
/// Regression tests for defects found in review of the initial JS target commit.
/// Each test names the original bug it locks against.
/// </summary>
public class JsReviewFixesTests
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

    // [P1] Local refs inside a nested $id resource must resolve within that
    // resource, not against the document root.
    [Fact]
    public void LocalRef_ResolvesWithinNestedIdResource()
    {
        Expect(
            """
            {
              "$defs": {
                "sub": {
                  "$id": "https://example.com/sub",
                  "$defs": { "x": { "type": "integer" } },
                  "$ref": "#/$defs/x"
                }
              },
              "$ref": "#/$defs/sub"
            }
            """,
            [
                ("42", true),
                ("\"not an integer\"", false),
                ("1.5", false),
            ]);
    }

    // [P2] Gate must not treat keyword names inside annotation/data (default,
    // examples, enum, const) as if they were schema keywords.
    [Fact]
    public void Gate_DoesNotDescendIntoDefault()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "type": "object",
              "default": { "unevaluatedProperties": false }
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected schema: {result.Error}");
    }

    [Fact]
    public void Gate_DoesNotDescendIntoEnum()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "enum": [ { "$ref": "https://external.example/x" }, { "unevaluatedItems": false } ]
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected schema: {result.Error}");
    }

    [Fact]
    public void Gate_DoesNotDescendIntoConst()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            { "const": { "$dynamicRef": "#meta" } }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected schema: {result.Error}");
    }

    // [P2] Relative $id must not crash — fall back to sourcePath for filename.
    [Fact]
    public void RelativeId_DoesNotCrash_FallsBackToSourcePath()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            { "$id": "person", "type": "object" }
            """).RootElement;
        var result = gen.Generate(schema, sourcePath: "/tmp/myschema.json");
        Assert.True(result.Success, result.Error);
        Assert.Equal("myschema.js", result.FileName);
    }

    [Fact]
    public void RelativeId_WithoutSourcePath_UsesDefault()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            { "$id": "schemas/person.json", "type": "object" }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, result.Error);
        Assert.Equal("validator.js", result.FileName);
    }

    // [P2] const was added in Draft 6 — Draft 4 must ignore it.
    [Fact]
    public void Const_IsIgnored_InDraft4()
    {
        Expect(
            """{ "$schema": "http://json-schema.org/draft-04/schema#", "const": 1 }""",
            [
                ("1", true),
                ("2", true),
                ("\"anything\"", true),
            ]);
    }

    [Fact]
    public void Const_IsEnforced_InDraft202012()
    {
        Expect(
            """{ "$schema": "https://json-schema.org/draft/2020-12/schema", "const": 1 }""",
            [
                ("1", true),
                ("2", false),
            ]);
    }

    // [P2] propertyNames is Draft 6+ — Draft 4 must ignore it.
    [Fact]
    public void PropertyNames_IsIgnored_InDraft4()
    {
        Expect(
            """
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "propertyNames": { "enum": ["x"] }
            }
            """,
            [
                ("{\"y\": 1}", true),
                ("{\"x\": 1}", true),
            ]);
    }

    [Fact]
    public void PropertyNames_IsEnforced_InDraft202012()
    {
        Expect(
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "propertyNames": { "enum": ["x"] }
            }
            """,
            [
                ("{\"x\": 1}", true),
                ("{\"y\": 1}", false),
            ]);
    }

    // [P2] if/then/else are Draft 7+ — Draft 4 must ignore them.
    [Fact]
    public void IfThenElse_IsIgnored_InDraft4()
    {
        Expect(
            """
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "if": {}, "then": false
            }
            """,
            [
                ("42", true),
                ("\"anything\"", true),
            ]);
    }

    [Fact]
    public void IfThenElse_IsEnforced_InDraft202012()
    {
        Expect(
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "if": { "type": "string" }, "then": { "minLength": 2 }
            }
            """,
            [
                ("\"ab\"", true),
                ("\"a\"", false),
                ("42", true),
            ]);
    }

    // [P2] Time format must range-check hour/minute/second, not just shape.
    [Fact]
    public void Time_RejectsOutOfRangeComponents()
    {
        Expect(
            """{ "format": "time" }""",
            [
                ("\"12:34:56Z\"", true),
                ("\"23:59:60Z\"", true),   // leap second
                ("\"25:00:00Z\"", false),
                ("\"23:61:00Z\"", false),
                ("\"23:59:61Z\"", false),
                ("\"12:34:56+14:00\"", true),
                ("\"12:34:56+25:00\"", false),
                ("\"12:34:56+10:60\"", false),
            ]);
    }

    [Fact]
    public void DateTime_RejectsOutOfRangeTimeComponents()
    {
        Expect(
            """{ "format": "date-time" }""",
            [
                ("\"2023-01-15T12:00:00Z\"", true),
                ("\"2023-01-15T25:00:00Z\"", false),
                ("\"2023-01-15T23:61:00Z\"", false),
            ]);
    }
}
