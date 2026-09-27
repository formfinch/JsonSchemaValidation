// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;
using Jint;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

/// <summary>
/// Extended format vectors (shared with the runtime and compiled C# tests) loaded from
/// <c>TestData/Formats</c>.
/// </summary>
public static class ExtendedFormatVectors
{
    public static string DirectoryPath => Path.Combine(AppContext.BaseDirectory, "TestData", "Formats");

    public static IEnumerable<object[]> All()
    {
        foreach (var file in Directory.GetFiles(DirectoryPath, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var group in doc.RootElement.EnumerateArray())
            {
                var schema = group.GetProperty("schema").GetRawText();
                foreach (var test in group.GetProperty("tests").EnumerateArray())
                {
                    yield return
                    [
                        Path.GetFileNameWithoutExtension(file),
                        test.GetProperty("description").GetString()!,
                        schema,
                        test.GetProperty("data").GetRawText(),
                        test.GetProperty("valid").GetBoolean(),
                    ];
                }
            }
        }
    }

    public static IEnumerable<string> DistinctSchemas() =>
        All().Select(row => (string)row[2]).Distinct(StringComparer.Ordinal);
}

public class ExtendedFormatJsTests
{
    private static readonly JsValidatorHarness WithCatalog = new(formatAssertionEnabled: true, extendedFormats: true);
    private static readonly JsValidatorHarness WithoutCatalog = new(formatAssertionEnabled: true);

    [Theory]
    [MemberData(nameof(ExtendedFormatVectors.All), MemberType = typeof(ExtendedFormatVectors))]
    public void Vector_WithCatalog_MatchesExpectation(string file, string description, string schema, string data, bool valid)
    {
        var result = WithCatalog.Evaluate(schema, [data]);

        Assert.True(result.Success, result.Error);
        Assert.True(result.Verdicts[0] == valid, $"{file}: {description} ({data}) expected valid={valid}");
    }

    [Theory]
    [MemberData(nameof(ExtendedFormatVectors.All), MemberType = typeof(ExtendedFormatVectors))]
    public void Vector_WithoutCatalog_IsAnnotationOnly(string file, string description, string schema, string data, bool valid)
    {
        _ = valid;
        var result = WithoutCatalog.Evaluate(schema, [data]);

        Assert.True(result.Success, result.Error);
        Assert.True(result.Verdicts[0], $"{file}: {description} ({data}) should pass while the catalog is off");
    }

    [Fact]
    public void Draft202012_WithoutFormatAssertion_IsAnnotationOnly()
    {
        var harness = new JsValidatorHarness(extendedFormats: true);
        var result = harness.Evaluate(
            """{ "$schema": "https://json-schema.org/draft/2020-12/schema", "format": "nl-phone" }""",
            ["\"not a phone number\""]);

        Assert.True(result.Success, result.Error);
        Assert.True(result.Verdicts[0]);
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-04/schema#")]
    [InlineData("https://json-schema.org/draft/2019-09/schema")]
    public void EarlierDrafts_AssertCatalogFormatsLikeBuiltIns(string draft)
    {
        var harness = new JsValidatorHarness(extendedFormats: true);
        var result = harness.Evaluate(
            $$"""{ "$schema": "{{draft}}", "format": "iso-13616-iban" }""",
            ["\"NL91 ABNA 0417 1643 00\"", "\"NL91 ABNA 0417 1643 01\""]);

        Assert.True(result.Success, result.Error);
        Assert.Equal([true, false], result.Verdicts);
    }

    [Fact]
    public void GeneratedCode_ImportsCatalogFunctionOnlyWhenEnabled()
    {
        using var schema = JsonDocument.Parse("""{ "$schema": "https://json-schema.org/draft/2020-12/schema", "format": "nl-phone" }""");

        var on = new JsSchemaCodeGenerator { FormatAssertionEnabled = true, ExtendedFormats = true }.Generate(schema.RootElement.Clone());
        var off = new JsSchemaCodeGenerator { FormatAssertionEnabled = true }.Generate(schema.RootElement.Clone());

        Assert.True(on.Success, on.Error);
        Assert.True(off.Success, off.Error);
        Assert.Contains("isValidNlPhone", on.GeneratedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("isValidNlPhone", off.GeneratedCode, StringComparison.Ordinal);
    }
}

/// <summary>
/// Generates a TS validator per distinct vector schema and compiles them with tsc once.
/// </summary>
public sealed class TsExtendedFormatFixture : IDisposable
{
    private readonly Dictionary<string, string> _moduleBySchema = new(StringComparer.Ordinal);

    public TsExtendedFormatFixture()
    {
        if (!TypeScriptCompiler.IsAvailable())
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "TypeScript compiler 'tsc' is required for the TS extended format tests.");
        }

        ModuleRoot = Path.Combine(Path.GetTempPath(), "jsv-ts-extended-formats-" + Guid.NewGuid().ToString("N"));
        var sourceRoot = Path.Combine(ModuleRoot, "ts-src");
        Directory.CreateDirectory(sourceRoot);
        var runtimePath = Path.Combine(sourceRoot, TsRuntime.FileName);
        File.WriteAllText(runtimePath, TsRuntime.GetSource());

        var generator = new TsSchemaCodeGenerator { FormatAssertionEnabled = true, ExtendedFormats = true };
        var sources = new List<string> { runtimePath };
        var index = 0;
        foreach (var schema in ExtendedFormatVectors.DistinctSchemas())
        {
            using var doc = JsonDocument.Parse(schema);
            var result = generator.Generate(doc.RootElement.Clone(), sourcePath: $"validator_{index}.json");
            if (!result.Success)
            {
                throw new InvalidOperationException($"TS codegen failed for {schema}: {result.Error}");
            }

            var sourcePath = Path.Combine(sourceRoot, $"validator_{index}.ts");
            File.WriteAllText(sourcePath, result.GeneratedCode!);
            sources.Add(sourcePath);
            _moduleBySchema[schema] = $"validator_{index}.js";
            index++;
        }

        var compile = TypeScriptCompiler.Compile(sources, ModuleRoot, ecmaScriptTarget: "ES2020", timeoutMilliseconds: 240_000);
        if (!compile.Success)
        {
            throw new InvalidOperationException(
                $"TypeScript compilation failed: {compile.Error}\n{compile.StandardError}\n{compile.StandardOutput}");
        }
    }

    public string ModuleRoot { get; }

    public string GetModule(string schema) => _moduleBySchema[schema];

    public void Dispose()
    {
        try { Directory.Delete(ModuleRoot, recursive: true); } catch { /* best effort */ }
    }
}

public class ExtendedFormatTsTests : IClassFixture<TsExtendedFormatFixture>
{
    private readonly TsExtendedFormatFixture _fixture;

    public ExtendedFormatTsTests(TsExtendedFormatFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [MemberData(nameof(ExtendedFormatVectors.All), MemberType = typeof(ExtendedFormatVectors))]
    public void Vector_CompiledTypeScript_MatchesExpectation(string file, string description, string schema, string data, bool valid)
    {
        var engine = new Engine(opts => opts.EnableModules(_fixture.ModuleRoot));
        var validate = engine.Modules.Import("./" + _fixture.GetModule(schema)).Get("validate");
        var parsed = engine.Evaluate($"JSON.parse({JsTestHelpers.ToJsStringLiteral(data)})");

        var verdict = engine.Invoke(validate, parsed);

        Assert.True(verdict.IsBoolean(), $"Non-boolean verdict for {file}: {description}");
        Assert.True(verdict.AsBoolean() == valid, $"{file}: {description} ({data}) expected valid={valid}");
    }
}
