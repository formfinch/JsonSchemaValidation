// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using Jint;
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

    // [P2] Gate is draft-aware. Post-Draft-4 keywords like unevaluatedProperties
    // or $dynamicRef are unknown keywords in Draft 4 and must be ignored, not
    // rejected. Schema-valued keywords that don't exist in Draft 4 (propertyNames,
    // if/then/else, contains, dependentSchemas, $defs) must not be traversed.
    [Fact]
    public void Gate_Draft4_AcceptsUnevaluatedProperties_AsUnknownKeyword()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "unevaluatedProperties": false
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected Draft 4 schema: {result.Error}");
    }

    [Fact]
    public void Gate_Draft4_AcceptsDynamicRef_AsUnknownKeyword()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "$dynamicRef": "#meta"
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected Draft 4 schema: {result.Error}");
    }

    [Fact]
    public void Gate_Draft4_DoesNotTraverseIntoPropertyNames()
    {
        // propertyNames is unknown in Draft 4. Even if its value contains a
        // keyword that WOULD be deferred in 2020-12, Draft 4 must ignore
        // the whole structure.
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "propertyNames": { "unevaluatedProperties": false }
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected Draft 4 schema: {result.Error}");
    }

    [Fact]
    public void Gate_Draft4_DoesNotTraverseIntoIfThenElse()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "if": { "$dynamicRef": "#x" },
              "then": { "unevaluatedItems": false }
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected Draft 4 schema: {result.Error}");
    }

    [Fact]
    public void Gate_Draft4_DoesNotTraverseIntoContains()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "contains": { "$recursiveRef": "#x" }
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected Draft 4 schema: {result.Error}");
    }

    [Fact]
    public void Gate_Draft4_DoesNotTraverseIntoDefs()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "$defs": { "a": { "unevaluatedProperties": false } }
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected Draft 4 schema: {result.Error}");
    }

    // Draft 2020-12 behavior is unchanged — must still reject real deferred
    // features when the keyword exists in the detected draft.
    [Fact]
    public void Gate_Draft202012_StillRejectsUnevaluatedProperties()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "unevaluatedProperties": false
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.False(result.Success);
        Assert.Contains("unevaluatedProperties", result.Error);
    }

    [Fact]
    public void Gate_Draft202012_StillRejectsDynamicRef()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$dynamicRef": "#meta"
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.False(result.Success);
        Assert.Contains("$dynamicRef", result.Error);
    }

    // Copilot review: enum/const values with U+2028/U+2029 must be escaped when
    // inlined into the emitted JS expression (legal in ES2019+ string literals
    // but defensive escaping protects older tooling).
    [Fact]
    public void Enum_EscapesLineSeparator()
    {
        Expect(
            "{ \"enum\": [\"a\u2028b\"] }",
            [
                ("\"a\u2028b\"", true),
                ("\"ab\"", false),
            ]);
    }

    [Fact]
    public void Const_EscapesParagraphSeparator()
    {
        Expect(
            "{ \"const\": \"x\u2029y\" }",
            [
                ("\"x\u2029y\"", true),
                ("\"xy\"", false),
            ]);
    }

    // Copilot review: empty patternProperties must not emit an object-keys loop.
    [Fact]
    public void PatternProperties_EmptyObject_EmitsNothing()
    {
        var result = _harness.Evaluate(
            """{ "patternProperties": {} }""",
            ["{\"anything\": 1}", "\"string\"", "42"]);
        Assert.True(result.Success, result.Error);
        Assert.All(result.Verdicts, v => Assert.True(v));
        // The dead object-keys loop should not appear.
        Assert.DoesNotContain("for (const _k of Object.keys", result.GeneratedSource);
    }

    // Copilot review: items as an array is invalid in Draft 2020-12 (replaced
    // by prefixItems). Codegen must fail loudly rather than silently compile.
    [Fact]
    public void Items_AsArray_InDraft202012_FailsCodegen()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "items": [ { "type": "integer" }, { "type": "string" } ]
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.False(result.Success);
        Assert.Contains("array-form \"items\"", result.Error);
    }

    [Fact]
    public void Items_AsArray_InDraft4_StillValid()
    {
        // Draft 4 accepts the tuple form of items — regression to ensure the
        // 2020-12-only guard doesn't misfire on older drafts.
        Expect(
            """
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "items": [ { "type": "integer" }, { "type": "string" } ]
            }
            """,
            [
                ("[1, \"a\"]", true),
                ("[\"x\", \"a\"]", false),
                ("[1, 2]", false),
                ("[]", true),
            ]);
    }

    // Copilot review: empty pattern string is an invalid schema per the
    // dynamic validator's stance; codegen should reject it.
    [Fact]
    public void Pattern_Empty_FailsCodegen()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""{ "pattern": "" }""").RootElement;
        var result = gen.Generate(schema);
        Assert.False(result.Success);
        Assert.Contains("empty \"pattern\"", result.Error);
    }

    // Copilot review: isInteger must not accept BigInt, since numeric-constraint
    // emitters only handle `typeof v === "number"`. Accepting BigInt in the type
    // check would let values silently skip minimum/maximum/multipleOf.
    [Fact]
    public void Integer_RejectsBigInt_SoNumericConstraintsApplyConsistently()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            { "type": "integer", "minimum": 1 }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, result.Error);

        var tempDir = Path.Combine(Path.GetTempPath(), "jsv-bigint-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(
                Path.Combine(tempDir, FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime.JsRuntime.FileName),
                FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime.JsRuntime.GetSource());
            File.WriteAllText(Path.Combine(tempDir, "validator.js"), result.GeneratedCode!);

            var engine = new Jint.Engine(opts => opts.EnableModules(tempDir));
            var module = engine.Modules.Import("./validator.js");
            var validate = module.Get("validate");
            var verdict = engine.Invoke(validate, engine.Evaluate("5n"));
            Assert.True(verdict.IsBoolean());
            // BigInt 5n would pass minimum=1 if isInteger returned true, but with
            // the fix it's rejected at the type check.
            Assert.False(verdict.AsBoolean());
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }
}
