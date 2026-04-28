// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;
using Jint;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public class TsSchemaCodeGeneratorTests
{
    private readonly TsSchemaCodeGenerator _generator = new();

    [Fact]
    public void Generate_SimpleSchema_EmitsTypeScriptModule()
    {
        var result = _generator.Generate(JsonDocument.Parse("""{ "type": "string" }""").RootElement);

        Assert.True(result.Success, result.Error);
        Assert.Equal("validator.ts", result.FileName);
        Assert.Contains("TypeScript target", result.GeneratedCode);
        Assert.DoesNotContain("@ts-nocheck", result.GeneratedCode);
        Assert.Contains("type JsonValue", result.GeneratedCode);
        Assert.Contains("export function validate(data: JsonValue): boolean", result.GeneratedCode);
        Assert.Contains("export default", result.GeneratedCode);
    }

    [Fact]
    public void Generate_PreservesRuntimeImportSpecifierForTscOutput()
    {
        var result = _generator.Generate(JsonDocument.Parse("""{ "minLength": 2 }""").RootElement);

        Assert.True(result.Success, result.Error);
        Assert.Contains("""from "./jsv-runtime.js";""", result.GeneratedCode);
    }

    [Fact]
    public void Compile_WithTscAvailable_ProducesExecutableJavaScript()
    {
        if (!TypeScriptCompiler.IsAvailable())
        {
            throw Xunit.Sdk.SkipException.ForSkip("TypeScript compiler 'tsc' is required for this test.");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "jsv-ts-test-" + Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempDir, "out");
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = _generator.Generate(JsonDocument.Parse("""{ "type": "integer", "minimum": 3 }""").RootElement);
            Assert.True(result.Success, result.Error);

            var validatorPath = Path.Combine(tempDir, result.FileName!);
            var runtimePath = Path.Combine(tempDir, TsRuntime.FileName);
            File.WriteAllText(validatorPath, result.GeneratedCode);
            File.WriteAllText(runtimePath, TsRuntime.GetSource());

            var compileResult = TypeScriptCompiler.Compile(
                [validatorPath, runtimePath],
                outputDir,
                ecmaScriptTarget: "ES2020");

            Assert.True(compileResult.Success, compileResult.Error);

            var engine = new Engine(opts => opts.EnableModules(outputDir));
            var module = engine.Modules.Import("./validator.js");
            var validate = module.Get("validate");

            Assert.True(engine.Invoke(validate, 4).AsBoolean());
            Assert.False(engine.Invoke(validate, 2).AsBoolean());
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Compile_WithTscAvailable_SupportsLowerEcmaScriptTarget()
    {
        if (!TypeScriptCompiler.IsAvailable())
        {
            throw Xunit.Sdk.SkipException.ForSkip("TypeScript compiler 'tsc' is required for this test.");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "jsv-ts-es5-test-" + Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempDir, "out");
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = _generator.Generate(JsonDocument.Parse("""{ "type": "string" }""").RootElement);
            Assert.True(result.Success, result.Error);

            var validatorPath = Path.Combine(tempDir, result.FileName!);
            var runtimePath = Path.Combine(tempDir, TsRuntime.FileName);
            File.WriteAllText(validatorPath, result.GeneratedCode);
            File.WriteAllText(runtimePath, TsRuntime.GetSource());

            var compileResult = TypeScriptCompiler.Compile(
                [validatorPath, runtimePath],
                outputDir,
                ecmaScriptTarget: "ES5");

            Assert.True(compileResult.Success, compileResult.Error);
            Assert.True(File.Exists(Path.Combine(outputDir, "validator.js")));
            Assert.True(File.Exists(Path.Combine(outputDir, "jsv-runtime.js")));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Compile_WithTscAvailable_FollowsPointerRefInsideNestedResource()
    {
        if (!TypeScriptCompiler.IsAvailable())
        {
            throw Xunit.Sdk.SkipException.ForSkip("TypeScript compiler 'tsc' is required for this test.");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "jsv-ts-anchor-ref-test-" + Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempDir, "out");
        Directory.CreateDirectory(tempDir);
        try
        {
            using var schema = JsonDocument.Parse("""
                {
                  "properties": {
                    "x": {
                      "$id": "https://example.com/nested",
                      "$ref": "#/x-target",
                      "x-target": { "type": "string" }
                    }
                  }
                }
                """);
            var result = _generator.Generate(schema.RootElement.Clone());
            Assert.True(result.Success, result.Error);

            var validatorPath = Path.Combine(tempDir, result.FileName!);
            var runtimePath = Path.Combine(tempDir, TsRuntime.FileName);
            File.WriteAllText(validatorPath, result.GeneratedCode);
            File.WriteAllText(runtimePath, TsRuntime.GetSource());

            var compileResult = TypeScriptCompiler.Compile(
                [validatorPath, runtimePath],
                outputDir,
                ecmaScriptTarget: "ES2020");

            Assert.True(compileResult.Success, compileResult.Error);

            var engine = new Engine(opts => opts.EnableModules(outputDir));
            var module = engine.Modules.Import("./validator.js");
            var validate = module.Get("validate");
            var valid = engine.Evaluate("""JSON.parse("{\"x\":\"ok\"}")""");
            var invalid = engine.Evaluate("""JSON.parse("{\"x\":42}")""");

            Assert.True(engine.Invoke(validate, valid).AsBoolean());
            Assert.False(engine.Invoke(validate, invalid).AsBoolean());
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void RuntimeSource_IsAuthoredTypeScriptWithoutNoCheckPragma()
    {
        var source = TsRuntime.GetSource();

        Assert.DoesNotContain("@ts-nocheck", source);
        Assert.Contains("// jsv-runtime.ts", source);
        Assert.Contains("export type JsonValue", source);
        Assert.Contains("export class EvaluatedState", source);
    }

    [Fact]
    public void RuntimeSource_WithTscAvailable_CompilesWithStrictSettings()
    {
        if (!TypeScriptCompiler.IsAvailable())
        {
            throw Xunit.Sdk.SkipException.ForSkip("TypeScript compiler 'tsc' is required for this test.");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "jsv-ts-runtime-strict-test-" + Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempDir, "out");
        Directory.CreateDirectory(tempDir);
        try
        {
            var runtimePath = Path.Combine(tempDir, TsRuntime.FileName);
            File.WriteAllText(runtimePath, TsRuntime.GetSource());

            var compileResult = TypeScriptCompiler.Compile(
                [runtimePath],
                outputDir,
                ecmaScriptTarget: "ES2020",
                strict: true,
                noImplicitAny: true);

            Assert.True(compileResult.Success, compileResult.Error);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }
}
