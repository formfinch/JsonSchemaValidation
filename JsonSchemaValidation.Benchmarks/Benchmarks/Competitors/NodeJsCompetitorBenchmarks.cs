// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using FormFinch.JsonSchemaValidation.Benchmarks.Config;
using FormFinch.JsonSchemaValidation.Benchmarks.Infrastructure;
using FormFinch.JsonSchemaValidation.CodeGeneration.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript;
using FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Benchmarks.Competitors;

/// <summary>
/// Draft 2020-12 JavaScript benchmark comparison between the generated FormFinch
/// validator and Ajv, both executed through the shared Node.js host.
/// Batch mode amortizes IPC so the measurement is dominated by validator work.
/// </summary>
[Config(typeof(ThroughputConfig))]
public class NodeJsCompetitorBenchmarks
{
    private const int BatchSize = 1000;

    private NodeBenchmarkHost _ajvHost = null!;
    private NodeBenchmarkHost _formFinchJsHost = null!;
    private NodeBenchmarkHost _formFinchTsDerivedJsHost = null!;
    private string _generatedModuleDirectory = null!;

    [Params(SchemaComplexity.Simple, SchemaComplexity.Medium, SchemaComplexity.Complex, SchemaComplexity.Production)]
    public SchemaComplexity Complexity { get; set; }

    [Params(true, false)]
    public bool IsValidInstance { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var schemaJson = BenchmarkData.GetSchema(Complexity);
        var instanceJson = IsValidInstance
            ? BenchmarkData.GetValidInstance(Complexity)
            : BenchmarkData.GetInvalidInstance(Complexity);

        _generatedModuleDirectory = Path.Combine(
            Path.GetTempPath(),
            "ff-js-bench-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_generatedModuleDirectory);

        var directDirectory = Path.Combine(_generatedModuleDirectory, "direct");
        Directory.CreateDirectory(directDirectory);
        foreach (var artifact in GenerateDirectJavaScriptArtifacts(schemaJson))
        {
            File.WriteAllText(Path.Combine(directDirectory, artifact.RelativePath), artifact.Content);
        }

        var typeScriptDerivedDirectory = Path.Combine(_generatedModuleDirectory, "typescript");
        Directory.CreateDirectory(typeScriptDerivedDirectory);
        var typeScriptDerivedModulePath = GenerateTypeScriptDerivedJavaScript(schemaJson, typeScriptDerivedDirectory);

        _ajvHost = new NodeBenchmarkHost();
        _ajvHost.PrepareAjv(schemaJson);
        _ajvHost.PrepareData(instanceJson);

        _formFinchJsHost = new NodeBenchmarkHost();
        _formFinchJsHost.PrepareGeneratedValidator(Path.Combine(directDirectory, "validator.js"));
        _formFinchJsHost.PrepareData(instanceJson);

        _formFinchTsDerivedJsHost = new NodeBenchmarkHost();
        _formFinchTsDerivedJsHost.PrepareGeneratedValidator(typeScriptDerivedModulePath);
        _formFinchTsDerivedJsHost.PrepareData(instanceJson);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _ajvHost?.Dispose();
        _formFinchJsHost?.Dispose();
        _formFinchTsDerivedJsHost?.Dispose();

        if (!string.IsNullOrEmpty(_generatedModuleDirectory))
        {
            try
            {
                Directory.Delete(_generatedModuleDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize, Description = "Ajv 2020-12")]
    public int Ajv202012()
    {
        return _ajvHost.ValidatePreparedBatch(BatchSize);
    }

    [Benchmark(OperationsPerInvoke = BatchSize, Description = "FormFinch JS codegen")]
    public int FormFinchJsCodegen()
    {
        return _formFinchJsHost.ValidatePreparedBatch(BatchSize);
    }

    [Benchmark(OperationsPerInvoke = BatchSize, Description = "FormFinch TS-derived JS codegen")]
    public int FormFinchTypeScriptDerivedJsCodegen()
    {
        return _formFinchTsDerivedJsHost.ValidatePreparedBatch(BatchSize);
    }

    private static IReadOnlyList<GeneratedArtifact> GenerateDirectJavaScriptArtifacts(string schemaJson)
    {
        using var schemaDoc = JsonDocument.Parse(schemaJson);
        var target = new JavaScriptCodeGenerationTarget();
        var result = target.GenerateAsync(new CodeGenerationRequest
        {
            Schema = schemaDoc.RootElement.Clone(),
            Options = new JavaScriptCodeGenerationOptions
            {
                DefaultDraft = SchemaDraft.Draft202012,
                SourcePath = "benchmark-schema.json",
                OutputHints = new CodeGenerationOutputHints
                {
                    BaseFileName = "validator"
                }
            }
        }).GetAwaiter().GetResult();
        if (!result.Success)
        {
            var message = result.Diagnostics.Count > 0
                ? string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Message))
                : "Unknown error";
            throw new InvalidOperationException(
                $"Failed to generate JS benchmark validator: {message}");
        }

        return result.Artifacts;
    }

    private static string GenerateTypeScriptDerivedJavaScript(string schemaJson, string outputDirectory)
    {
        using var schemaDoc = JsonDocument.Parse(schemaJson);
        var generator = new TsSchemaCodeGenerator
        {
            DefaultDraft = SchemaDraft.Draft202012
        };

        var result = generator.Generate(schemaDoc.RootElement.Clone(), sourcePath: "validator.json");
        if (!result.Success || string.IsNullOrEmpty(result.GeneratedCode))
        {
            throw new InvalidOperationException(
                $"Failed to generate TS benchmark validator: {result.Error ?? "Unknown error"}");
        }

        var sourceDirectory = Path.Combine(outputDirectory, "ts-src");
        Directory.CreateDirectory(sourceDirectory);
        var validatorPath = Path.Combine(sourceDirectory, result.FileName!);
        var runtimePath = Path.Combine(sourceDirectory, TsRuntime.FileName);
        File.WriteAllText(validatorPath, result.GeneratedCode);
        File.WriteAllText(runtimePath, TsRuntime.GetSource());

        var compilationResult = TypeScriptCompiler.Compile(
            [validatorPath, runtimePath],
            outputDirectory,
            ecmaScriptTarget: "ES2020");
        if (!compilationResult.Success)
        {
            throw new InvalidOperationException(
                $"Failed to compile TS benchmark validator: {compilationResult.Error}\n{compilationResult.StandardError}\n{compilationResult.StandardOutput}");
        }

        var modulePath = Path.Combine(outputDirectory, Path.ChangeExtension(result.FileName!, ".js"));
        if (!File.Exists(modulePath))
        {
            throw new InvalidOperationException(
                $"TS benchmark validator compiled successfully, but the expected module was not produced: {modulePath}");
        }

        return modulePath;
    }
}
