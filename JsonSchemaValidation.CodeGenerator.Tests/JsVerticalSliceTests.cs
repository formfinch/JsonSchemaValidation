// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

/// <summary>
/// End-to-end execution tests for the vertical slice:
/// type, required, properties, pattern, minLength/maxLength.
/// Generates JS, executes via Jint against the shared runtime, and checks
/// verdicts to prove the emitter + runtime wiring is correct before we
/// expand keyword coverage.
/// </summary>
public class JsVerticalSliceTests
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
                $"Emitted source:\n{result.GeneratedSource}");
        }
    }

    [Fact]
    public void Type_String_RejectsNonStrings()
    {
        Expect(
            """{ "type": "string" }""",
            [
                ("\"hello\"", true),
                ("42", false),
                ("null", false),
                ("true", false),
                ("[]", false),
                ("{}", false),
            ]);
    }

    [Fact]
    public void Type_Integer_AcceptsIntegerValuedNumbers()
    {
        Expect(
            """{ "type": "integer" }""",
            [
                ("5", true),
                ("-17", true),
                ("0", true),
                ("5.0", true),
                ("5.5", false),
                ("\"5\"", false),
            ]);
    }

    [Fact]
    public void Type_Object_RejectsArraysAndNull()
    {
        Expect(
            """{ "type": "object" }""",
            [
                ("{}", true),
                ("{\"k\":1}", true),
                ("[]", false),
                ("null", false),
                ("42", false),
            ]);
    }

    [Fact]
    public void Type_Array_OfStrings_Or_Numbers()
    {
        Expect(
            """{ "type": ["string", "number"] }""",
            [
                ("\"x\"", true),
                ("42", true),
                ("true", false),
                ("null", false),
            ]);
    }

    [Fact]
    public void Required_OnlyAppliesToObjects()
    {
        Expect(
            """{ "required": ["name", "age"] }""",
            [
                ("{\"name\":\"a\",\"age\":1}", true),
                ("{\"name\":\"a\"}", false),
                ("{}", false),
                ("42", true),             // required is a no-op for non-objects
                ("[\"name\",\"age\"]", true),
            ]);
    }

    [Fact]
    public void Properties_RecursivelyValidatesEachKnownKey()
    {
        Expect(
            """
            {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "age": { "type": "integer" }
              }
            }
            """,
            [
                ("{\"name\":\"a\",\"age\":1}", true),
                ("{\"name\":123}", false),
                ("{\"age\":1.5}", false),
                ("{\"age\":1}", true),
                ("{}", true),             // no required constraint
            ]);
    }

    [Fact]
    public void Pattern_IsECMAScriptRegex_OnStrings()
    {
        Expect(
            """{ "pattern": "^[a-z]+$" }""",
            [
                ("\"hello\"", true),
                ("\"Hello\"", false),
                ("\"\"", false),
                ("42", true),             // non-strings bypass the pattern check
            ]);
    }

    [Fact]
    public void MinLength_And_MaxLength_CountGraphemes()
    {
        Expect(
            """{ "minLength": 2, "maxLength": 4 }""",
            [
                ("\"ab\"", true),
                ("\"abcd\"", true),
                ("\"a\"", false),
                ("\"abcde\"", false),
                // Emoji flag is a single grapheme cluster (2 code points).
                // Grapheme counter (when Intl.Segmenter available) says length 1,
                // so "🇳🇱" fails minLength 2. Fallback path counts code points = 2 (passes).
                // The harness asserts the Intl.Segmenter-aware verdict since Jint 4.8 ships it.
                ("\"\\uD83C\\uDDF3\\uD83C\\uDDF1\"", false),
            ]);
    }

    [Fact]
    public void RequiresProperties_Compose_TopLevel()
    {
        Expect(
            """
            {
              "type": "object",
              "required": ["id"],
              "properties": {
                "id": { "type": "string", "minLength": 1 }
              }
            }
            """,
            [
                ("{\"id\":\"abc\"}", true),
                ("{\"id\":\"\"}", false),     // minLength 1 fails
                ("{\"id\":1}", false),         // type string fails
                ("{}", false),                 // required id fails
                ("\"raw\"", false),            // type: object — string rejected at the type check
            ]);
    }

    [Fact]
    public void Probe_IntlSegmenter_IsAvailable_InJint()
    {
        // If Jint ever ships without Intl.Segmenter, this test fires first and
        // signals that the runtime's code-point fallback needs to be validated
        // against the test suite before broad keyword work proceeds.
        Assert.True(JsValidatorHarness.IntlSegmenterAvailable(),
            "Intl.Segmenter not present in this Jint build; runtime grapheme counter " +
            "will fall back to code-point counting — revisit before expanding coverage.");
    }
}
