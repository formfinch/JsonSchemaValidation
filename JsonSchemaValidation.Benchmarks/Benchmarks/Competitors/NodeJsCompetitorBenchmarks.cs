// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using FormFinch.JsonSchemaValidation.Benchmarks.Config;
using FormFinch.JsonSchemaValidation.Benchmarks.Infrastructure;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime;

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

        File.WriteAllText(Path.Combine(_generatedModuleDirectory, JsRuntime.FileName), JsRuntime.GetSource());
        File.WriteAllText(Path.Combine(_generatedModuleDirectory, "validator.js"), GenerateValidatorSource(schemaJson));

        _ajvHost = new NodeBenchmarkHost();
        _ajvHost.PrepareAjv(schemaJson);
        _ajvHost.PrepareData(instanceJson);

        _formFinchJsHost = new NodeBenchmarkHost();
        _formFinchJsHost.PrepareGeneratedValidator(Path.Combine(_generatedModuleDirectory, "validator.js"));
        _formFinchJsHost.PrepareData(instanceJson);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _ajvHost?.Dispose();
        _formFinchJsHost?.Dispose();

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

    private static string GenerateValidatorSource(string schemaJson)
    {
        using var schemaDoc = JsonDocument.Parse(schemaJson);
        var generator = new JsSchemaCodeGenerator
        {
            DefaultDraft = SchemaDraft.Draft202012
        };

        var result = generator.Generate(schemaDoc.RootElement.Clone(), sourcePath: "benchmark-schema.json");
        if (!result.Success || string.IsNullOrEmpty(result.GeneratedCode))
        {
            throw new InvalidOperationException(
                $"Failed to generate JS benchmark validator: {result.Error ?? "Unknown error"}");
        }

        return result.GeneratedCode;
    }
}
