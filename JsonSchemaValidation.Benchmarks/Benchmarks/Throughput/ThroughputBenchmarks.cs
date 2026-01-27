// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Benchmarks.Config;
using FormFinch.JsonSchemaValidation.Benchmarks.Infrastructure;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using Json.Schema;
using LateApexEarlySpeed.Json.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Benchmarks.Throughput;

/// <summary>
/// Throughput benchmarks measuring validations per second with batch operations.
/// </summary>
[Config(typeof(ThroughputConfig))]
[MemoryDiagnoser]
public class ThroughputBenchmarks
{
    private const int BatchSize = 1000;

    private string _instanceJson = null!;
    private JsonElement _instanceElement;
    private JsonNode? _instanceNode;

    // FormFinch
    private ServiceProvider _formFinchProvider = null!;
    private ISchemaValidator _formFinchValidator = null!;
    private IJsonValidationContextFactory _formFinchContextFactory = null!;

    // JsonSchema.Net
    private JsonSchema _jsonSchemaNet = null!;
    private EvaluationOptions _jsonSchemaNetOptions = null!;

    // LateApex
    private JsonValidator _lateApex = null!;

    [Params(SchemaComplexity.Simple, SchemaComplexity.Medium)]
    public SchemaComplexity Complexity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var schemaJson = BenchmarkData.GetSchema(Complexity);
        _instanceJson = BenchmarkData.GetValidInstance(Complexity);

        using var doc = JsonDocument.Parse(_instanceJson);
        _instanceElement = doc.RootElement.Clone();
        _instanceNode = JsonNode.Parse(_instanceJson);

        // Setup FormFinch
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt => opt.EnableDraft202012 = true);
        _formFinchProvider = services.BuildServiceProvider();
        _formFinchProvider.InitializeSingletonServices();

        var repository = _formFinchProvider.GetRequiredService<ISchemaRepository>();
        var factory = _formFinchProvider.GetRequiredService<ISchemaValidatorFactory>();
        _formFinchContextFactory = _formFinchProvider.GetRequiredService<IJsonValidationContextFactory>();

        using var schemaDoc = JsonDocument.Parse(schemaJson);
        repository.TryRegisterSchema(schemaDoc.RootElement.Clone(), out var schemaData);
        _formFinchValidator = factory.GetValidator(schemaData!.SchemaUri!);

        // Setup JsonSchema.Net
        _jsonSchemaNet = JsonSchema.FromText(schemaJson);
        _jsonSchemaNetOptions = new EvaluationOptions
        {
            OutputFormat = OutputFormat.Flag,
            RequireFormatValidation = false
        };

        // Setup LateApex
        _lateApex = new JsonValidator(schemaJson);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _formFinchProvider?.Dispose();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize, Description = "FormFinch")]
    public int FormFinch_ValidateBatch()
    {
        int validCount = 0;
        for (int i = 0; i < BatchSize; i++)
        {
            var context = _formFinchContextFactory.CreateContextForRoot(_instanceElement);
            if (_formFinchValidator.ValidateRoot(context).IsValid)
            {
                validCount++;
            }
        }
        return validCount;
    }

    [Benchmark(OperationsPerInvoke = BatchSize, Description = "JsonSchema.Net")]
    public int JsonSchemaNet_ValidateBatch()
    {
        int validCount = 0;
        for (int i = 0; i < BatchSize; i++)
        {
            var result = _jsonSchemaNet.Evaluate(_instanceNode, _jsonSchemaNetOptions);
            if (result.IsValid)
            {
                validCount++;
            }
        }
        return validCount;
    }

    [Benchmark(OperationsPerInvoke = BatchSize, Description = "LateApex")]
    public int LateApex_ValidateBatch()
    {
        int validCount = 0;
        for (int i = 0; i < BatchSize; i++)
        {
            if (_lateApex.Validate(_instanceJson).IsValid)
            {
                validCount++;
            }
        }
        return validCount;
    }
}
