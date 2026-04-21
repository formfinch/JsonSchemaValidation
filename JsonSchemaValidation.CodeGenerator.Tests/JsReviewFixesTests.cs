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
    // Copilot round 3: NaN/Infinity must not silently pass numeric constraints
    // or type: number. JSON.parse can't produce them, but direct API callers can.
    [Fact]
    public void Type_Number_RejectsNaN_ViaDirectApi()
    {
        AssertVerdictForJsExpr("""{ "type": "number" }""", "NaN", false);
        AssertVerdictForJsExpr("""{ "type": "number" }""", "Infinity", false);
        AssertVerdictForJsExpr("""{ "type": "number" }""", "-Infinity", false);
        AssertVerdictForJsExpr("""{ "type": "number" }""", "42", true);
    }

    [Fact]
    public void TypeNumber_Plus_Minimum_RejectsNaN()
    {
        // With type: number enforcing finiteness, NaN fails type before reaching
        // numeric constraints. Confirms the guard keeps NaN from slipping through.
        AssertVerdictForJsExpr("""{ "type": "number", "minimum": 0 }""", "NaN", false);
        AssertVerdictForJsExpr("""{ "type": "number", "minimum": 0 }""", "Infinity", false);
        AssertVerdictForJsExpr("""{ "type": "number", "minimum": 0 }""", "1", true);
        AssertVerdictForJsExpr("""{ "type": "number", "minimum": 0 }""", "-1", false);
    }

    [Fact]
    public void BareMinimum_SkipsNaN_AsUnknownType()
    {
        // Without type: number the schema doesn't restrict type, and JSON Schema
        // numeric constraints only apply to JSON numbers — NaN isn't one. Validator
        // returns true because no applicable constraint was violated.
        AssertVerdictForJsExpr("""{ "minimum": 0 }""", "NaN", true);
        AssertVerdictForJsExpr("""{ "minimum": 0 }""", "1", true);
        AssertVerdictForJsExpr("""{ "minimum": 0 }""", "-1", false);
    }

    [Fact]
    public void StringConstraints_DoNotEmitUnusedRuntimeImport()
    {
        // When minLength/maxLength are present but non-integer, GenerateCode
        // ignores them and must NOT import graphemeLength.
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            { "minLength": "not-an-integer" }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain("graphemeLength", result.GeneratedCode);
    }

    private static void AssertVerdictForJsExpr(string schemaJson, string dataJsExpr, bool expected)
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse(schemaJson).RootElement;
        var genResult = gen.Generate(schema);
        Assert.True(genResult.Success, genResult.Error);

        var tempDir = Path.Combine(Path.GetTempPath(), "jsv-direct-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(
                Path.Combine(tempDir, FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime.JsRuntime.FileName),
                FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime.JsRuntime.GetSource());
            File.WriteAllText(Path.Combine(tempDir, "validator.js"), genResult.GeneratedCode!);
            var engine = new Engine(opts => opts.EnableModules(tempDir));
            var module = engine.Modules.Import("./validator.js");
            var validate = module.Get("validate");
            var verdict = engine.Invoke(validate, engine.Evaluate(dataJsExpr));
            Assert.True(verdict.IsBoolean(),
                $"Non-boolean verdict for {dataJsExpr}. Source:\n{genResult.GeneratedCode}");
            Assert.True(verdict.AsBoolean() == expected,
                $"Data {dataJsExpr}: expected {expected}, got {verdict.AsBoolean()}.\n" +
                $"Source:\n{genResult.GeneratedCode}");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Integer_RejectsBigInt_SoNumericConstraintsApplyConsistently()
    {
        // BigInt would pass minimum=1 if isInteger returned true for it, but with
        // the fix it's rejected at the type check before numeric constraints run.
        AssertVerdictForJsExpr("""{ "type": "integer", "minimum": 1 }""", "5n", false);
    }

    // Copilot round 4: contentSchema is annotation-only in this codebase.
    // Gate must not traverse into it and reject on nested deferred keywords.
    [Fact]
    public void Gate_DoesNotTraverseIntoContentSchema()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "string",
              "contentSchema": { "unevaluatedProperties": false }
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate wrongly rejected schema: {result.Error}");
    }

    // Codex review: contentSchema metadata must not produce emitter failures
    // even when it contains keyword shapes that would be invalid as real
    // subschemas (e.g., items-as-array in Draft 2020-12). The reachability
    // walk skips annotation keywords, so no validator function is emitted for
    // the contentSchema contents.
    [Fact]
    public void ContentSchema_NotEmitted_EvenIfItContainsInvalidShapes()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "string",
              "contentSchema": { "items": [ { "type": "integer" } ] }
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"contentSchema metadata should not cause codegen failure: {result.Error}");
        Assert.DoesNotContain("validate_", result.GeneratedCode!.Replace($"validate_{SchemaHasher_ComputeRoot(schema)}", ""));
    }

    // Helper: hash of root schema (extractor keeps root under its own hash).
    private static string SchemaHasher_ComputeRoot(JsonElement root) =>
        FormFinch.JsonSchemaValidation.Common.SchemaHasher.ComputeHash(root);

    // Codex review: when two nested $id resources each contain a text-identical
    // $ref-only subschema, the shared analyzer collapses them to one hash and
    // retains only the first resource root. Resolving the ref in the second
    // context would then target the first resource's definition. The reachability
    // pass detects this and rejects pre-emission.
    [Fact]
    public void AmbiguousResourceRef_IsRejected()
    {
        // Both A and B nest an identical {"$ref":"#/$defs/T"} subschema. Each
        // resource defines its own T differently. The shared extractor collapses
        // the two ref objects to a single SubschemaInfo keyed on the first
        // resource, so the emitted validator would resolve "#/$defs/T" against
        // the wrong resource at one of the two call sites. Detector must reject.
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$defs": {
                "A": {
                  "$id": "https://example.com/a",
                  "$defs": { "T": { "type": "integer" } },
                  "allOf": [ { "$ref": "#/$defs/T" } ]
                },
                "B": {
                  "$id": "https://example.com/b",
                  "$defs": { "T": { "type": "string" } },
                  "allOf": [ { "$ref": "#/$defs/T" } ]
                }
              },
              "allOf": [
                { "$ref": "#/$defs/A" },
                { "$ref": "#/$defs/B" }
              ]
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.False(result.Success);
        Assert.Contains("multiple nested $id resources", result.Error);
    }

    // Copilot round 8: reachability must follow local $ref so a ref target under
    // a non-applicator container still ends up in the emitted module. Uses the
    // same "legacy definitions in 2020-12" shape as the round-6 test — the map
    // fix alone covers the known keyword; the follow-ref fix is a belt-and-
    // suspenders guarantee that the same pattern works even if the extractor
    // places the target under a container we don't explicitly walk.
    [Fact]
    public void LocalRef_MarksTargetReachable_EvenThroughLegacyContainer()
    {
        // If my applicator set missed "definitions" (it does now, but this test
        // exists to lock the follow-ref behavior regardless), the ref would still
        // have to resolve and the target's validate_<hash> would still have to
        // be emitted.
        Expect(
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "definitions": { "inner": { "type": "integer", "minimum": 0 } },
              "allOf": [ { "$ref": "#/definitions/inner" } ]
            }
            """,
            [
                ("5", true),
                ("-1", false),
                ("\"no\"", false),
            ]);
    }

    // Copilot round 7: schema numeric values that exceed long.MaxValue must not
    // silently cast to garbage longs. Out-of-range minLength/maxLength etc. are
    // treated as "no valid integer" and the constraint is skipped.
    [Fact]
    public void MinLength_WithOutOfRangeValue_IsIgnored_NoGarbageEmitted()
    {
        var gen = new JsSchemaCodeGenerator();
        // 1e20 fits in a double but overflows long. Before the fix this cast
        // produced an unspecified long and the emitted JS contained a bogus
        // comparison. After the fix, the constraint is treated as unparseable
        // and skipped entirely, so no length check appears in the output.
        var schema = JsonDocument.Parse("""{ "minLength": 1e20 }""").RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain("_len <", result.GeneratedCode);
        Assert.DoesNotContain("graphemeLength", result.GeneratedCode);
    }

    // Copilot round 6: legacy "definitions" used under Draft 2020-12. The
    // shared extractor walks it, so $ref targets under #/definitions must be
    // emitted as validators. If they aren't, the generated JS calls a
    // missing validate_<hash> at runtime.
    [Fact]
    public void Draft202012_LegacyDefinitions_WithRef_IsValidatedCorrectly()
    {
        Expect(
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "definitions": { "nonnegInt": { "type": "integer", "minimum": 0 } },
              "$ref": "#/definitions/nonnegInt"
            }
            """,
            [
                ("5", true),
                ("-1", false),
                ("\"x\"", false),
            ]);
    }

    // Copilot round 5: gate should mirror the emitter's $ref-siblings-masked
    // behavior for Draft 4-7 — any sibling keyword is ignored at emission, so
    // the gate shouldn't reject a schema because those ignored siblings contain
    // deferred features.
    [Fact]
    public void Gate_Draft4_IgnoresMaskedSiblingsAlongsideRef()
    {
        var gen = new JsSchemaCodeGenerator();
        // In Draft 4, $ref masks all siblings — including `not` here, which
        // would otherwise be traversed and trip on unevaluatedProperties.
        var schema = JsonDocument.Parse("""
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "definitions": { "x": { "type": "integer" } },
              "$ref": "#/definitions/x",
              "not": { "unevaluatedProperties": false }
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, $"Gate should ignore masked siblings: {result.Error}");
    }

    // Copilot round 5: regex literal form is brittle for patterns beginning
    // with '*' because /*.../ parses as a block-comment token. Generator now
    // uses new RegExp("...") construction which sidesteps the hazard entirely.
    // The emitted-source check covers ALL patterns, not just the leading-*
    // case — any schema-supplied pattern is now kept out of JS tokenisation.
    [Fact]
    public void PatternEmission_UsesConstructorFormNotLiteralSlash()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            { "patternProperties": { "^a": { "type": "integer" } } }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, result.Error);
        Assert.Contains("new RegExp(", result.GeneratedCode);
        Assert.DoesNotContain("/^a/", result.GeneratedCode);
    }

    // Copilot round 5: patternProperties must hoist RegExp allocation outside
    // the per-property loop. Without hoisting, each property name allocates a
    // new RegExp on every validate call.
    [Fact]
    public void PatternProperties_HoistsRegexOutsideLoop()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "patternProperties": {
                "^a": { "type": "integer" },
                "^b": { "type": "string" }
              }
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, result.Error);
        var src = result.GeneratedCode!;
        // Both regexes declared as consts before the for-loop.
        var hoistIndex = src.IndexOf("const _ppRe", StringComparison.Ordinal);
        var forIndex = src.IndexOf("for (const _k of Object.keys", StringComparison.Ordinal);
        Assert.True(hoistIndex > 0 && forIndex > hoistIndex,
            "Hoisted RegExp constants must appear before the Object.keys loop.");
    }

    // Non-ambiguous case: two resources with different $ref text — not collapsed,
    // and each resolves against its own resource. Regression guard that the
    // ambiguity detector doesn't misfire on schemas that are actually fine.
    [Fact]
    public void DistinctRefsAcrossResources_AreAllowed()
    {
        var gen = new JsSchemaCodeGenerator();
        var schema = JsonDocument.Parse("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$defs": {
                "A": {
                  "$id": "https://example.com/a",
                  "$defs": { "Ta": { "type": "integer" } },
                  "$ref": "#/$defs/Ta"
                },
                "B": {
                  "$id": "https://example.com/b",
                  "$defs": { "Tb": { "type": "string" } },
                  "$ref": "#/$defs/Tb"
                }
              },
              "allOf": [
                { "$ref": "#/$defs/A" },
                { "$ref": "#/$defs/B" }
              ]
            }
            """).RootElement;
        var result = gen.Generate(schema);
        Assert.True(result.Success, result.Error);
    }
}
