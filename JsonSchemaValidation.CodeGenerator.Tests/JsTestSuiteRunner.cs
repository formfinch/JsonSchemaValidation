// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime;
using Jint;
using Xunit;
using Xunit.Abstractions;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

/// <summary>
/// Runs the MVP-relevant subset of the official JSON Schema Test Suite against
/// the JS emitter + Jint runtime. Supplements the explicit-verdict tests by
/// confirming the emitter tracks spec behavior on a wide range of real schemas.
///
/// Schemas that trip the JS capability gate (deferred features like external
/// $ref or unevaluated*) are counted as legitimate skips, not failures — the
/// gate is doing its job.
/// </summary>
public class JsTestSuiteRunner
{
    private readonly ITestOutputHelper _output;
    public JsTestSuiteRunner(ITestOutputHelper output) => _output = output;

    // MVP keyword coverage: one file per keyword family. Drives enough breadth
    // to catch emitter regressions without turning this into a whole-suite run.
    private static readonly string[] Draft202012Files =
    [
        "type.json",
        "required.json",
        "properties.json",
        "enum.json",
        "const.json",
        "minLength.json",
        "maxLength.json",
        "pattern.json",
        "minimum.json",
        "maximum.json",
        "exclusiveMinimum.json",
        "exclusiveMaximum.json",
        "multipleOf.json",
        "minItems.json",
        "maxItems.json",
        "uniqueItems.json",
        "minProperties.json",
        "maxProperties.json",
        "minContains.json",
        "maxContains.json",
        "items.json",
        "prefixItems.json",
        "contains.json",
        "patternProperties.json",
        "additionalProperties.json",
        "propertyNames.json",
        "allOf.json",
        "anyOf.json",
        "oneOf.json",
        "not.json",
        "if-then-else.json",
        "dependentRequired.json",
        "dependentSchemas.json",
        "boolean_schema.json",
        "ref.json",
        "defs.json",
        // format.json is deliberately excluded: the suite expects annotation-only
        // behavior by default (2020-12 spec), while our compiled path eager-validates
        // supported formats — same stance as the C# compiled tests. Explicit format
        // coverage lives in JsFormatTests.
    ];

    private static readonly string[] Draft4Files =
    [
        "type.json",
        "required.json",
        "properties.json",
        "enum.json",
        "minLength.json",
        "maxLength.json",
        "pattern.json",
        "minimum.json",
        "maximum.json",
        "multipleOf.json",
        "minItems.json",
        "maxItems.json",
        "uniqueItems.json",
        "minProperties.json",
        "maxProperties.json",
        "items.json",
        "patternProperties.json",
        "additionalProperties.json",
        "allOf.json",
        "anyOf.json",
        "oneOf.json",
        "not.json",
        "dependencies.json",
    ];

    // Test-description substrings whose cases we intentionally bypass — each
    // reason is called out so future-me knows what a bypass covers. Anything
    // listed here is material worth reviewing when expanding scope.
    private static readonly (string Contains, string Why)[] CaseSkips =
    [
        ("remote ref", "Gate rejects external refs — deferred feature."),
        ("base URI change", "Gate rejects cross-document refs — deferred feature."),
    ];

    [Theory]
    [MemberData(nameof(Draft202012Cases))]
    public void Draft202012(TestCase tc) => RunCase(tc);

    [Theory]
    [MemberData(nameof(Draft4Cases))]
    public void Draft4(TestCase tc) => RunCase(tc);

    public static IEnumerable<object[]> Draft202012Cases() =>
        EnumerateCases("draft2020-12", Draft202012Files, SchemaDraft.Draft202012);

    public static IEnumerable<object[]> Draft4Cases() =>
        EnumerateCases("draft4", Draft4Files, SchemaDraft.Draft4);

    private static IEnumerable<object[]> EnumerateCases(
        string draftFolder,
        string[] files,
        SchemaDraft draft)
    {
        var suitePath = FindTestSuitePath();
        if (suitePath == null) yield break;

        foreach (var file in files)
        {
            var full = Path.Combine(suitePath, "tests", draftFolder, file);
            if (!File.Exists(full)) continue;

            var json = File.ReadAllText(full);
            using var doc = JsonDocument.Parse(json);
            foreach (var group in doc.RootElement.EnumerateArray())
            {
                var groupDesc = group.GetProperty("description").GetString() ?? "";
                var schema = group.GetProperty("schema").GetRawText();
                foreach (var test in group.GetProperty("tests").EnumerateArray())
                {
                    var testDesc = test.GetProperty("description").GetString() ?? "";
                    var data = test.GetProperty("data").GetRawText();
                    var valid = test.GetProperty("valid").GetBoolean();
                    yield return [new TestCase(draft, file, groupDesc, testDesc, schema, data, valid)];
                }
            }
        }
    }

    private void RunCase(TestCase tc)
    {
        foreach (var (contains, why) in CaseSkips)
        {
            if (tc.GroupDescription.Contains(contains, StringComparison.OrdinalIgnoreCase) ||
                tc.TestDescription.Contains(contains, StringComparison.OrdinalIgnoreCase))
            {
                _output.WriteLine($"SKIP: {tc} — {why}");
                return;
            }
        }

        var generator = new JsSchemaCodeGenerator { DefaultDraft = tc.Draft };
        var schemaElement = JsonDocument.Parse(tc.SchemaJson).RootElement;
        var genResult = generator.Generate(schemaElement);
        if (!genResult.Success)
        {
            // Gate rejection = legitimate skip for MVP.
            if (genResult.Error!.Contains("JS target MVP") || genResult.Error.Contains("external $ref"))
            {
                _output.WriteLine($"SKIP (gate): {tc} — {genResult.Error}");
                return;
            }
            Assert.Fail($"Codegen failed for {tc}: {genResult.Error}");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "jsv-suite-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, JsRuntime.FileName), JsRuntime.GetSource());
            File.WriteAllText(Path.Combine(tempDir, "validator.js"), genResult.GeneratedCode!);
            var engine = new Engine(opts => opts.EnableModules(tempDir));
            var module = engine.Modules.Import("./validator.js");
            var validate = module.Get("validate");

            var parsed = engine.Evaluate($"JSON.parse({ToJsStringLiteral(tc.DataJson)})");
            var verdictRaw = engine.Invoke(validate, parsed);
            Assert.True(verdictRaw.IsBoolean(), $"Non-boolean verdict for {tc}");
            var actual = verdictRaw.AsBoolean();
            Assert.True(actual == tc.Expected,
                $"{tc}\nExpected: {tc.Expected}, Got: {actual}\nSchema: {tc.SchemaJson}\nData: {tc.DataJson}\nSource:\n{genResult.GeneratedCode}");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static string? FindTestSuitePath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir != null; i++)
        {
            var candidate = Path.Combine(dir, "submodules", "JSON-Schema-Test-Suite");
            if (Directory.Exists(Path.Combine(candidate, "tests"))) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private static string ToJsStringLiteral(string input)
    {
        return "\"" + input
            .Replace("\u2028", "\\u2028")
            .Replace("\u2029", "\\u2029")
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r") + "\"";
    }

    public sealed record TestCase(
        SchemaDraft Draft,
        string File,
        string GroupDescription,
        string TestDescription,
        string SchemaJson,
        string DataJson,
        bool Expected)
    {
        public override string ToString() =>
            $"[{Draft}/{File}] {GroupDescription} -> {TestDescription}";
    }
}
