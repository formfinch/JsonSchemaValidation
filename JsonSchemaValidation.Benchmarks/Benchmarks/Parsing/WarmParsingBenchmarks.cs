// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Benchmarks.Config;
using FormFinch.JsonSchemaValidation.Benchmarks.Infrastructure;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using Json.Schema;
using LateApexEarlySpeed.Json.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Benchmarks.Parsing;

/// <summary>
/// Warm parsing benchmarks that measure schema parsing with pre-warmed infrastructure.
/// The DI container and factories are initialized once; only schema parsing is measured.
/// </summary>
[Config(typeof(DefaultConfig))]
[MemoryDiagnoser]
public class WarmParsingBenchmarks
{
    private string _schemaJson = null!;
    private ServiceProvider _provider = null!;
    private ISchemaRepository _repository = null!;
    private ISchemaValidatorFactory _factory = null!;
    private int _schemaCounter;

    [Params(SchemaComplexity.Simple, SchemaComplexity.Medium, SchemaComplexity.Complex)]
    public SchemaComplexity Complexity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _schemaJson = BenchmarkData.GetSchema(Complexity);

        // Pre-warm FormFinch infrastructure
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt => opt.EnableDraft202012 = true);
        _provider = services.BuildServiceProvider();
        _provider.InitializeSingletonServices();
        _repository = _provider.GetRequiredService<ISchemaRepository>();
        _factory = _provider.GetRequiredService<ISchemaValidatorFactory>();

        // Warm up JsonSchema.Net
        _ = JsonSchema.FromText(_schemaJson);

        // Warm up LateApex
        _ = new JsonValidator(_schemaJson);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _provider?.Dispose();
    }

    [Benchmark(Baseline = true, Description = "FormFinch")]
    public object FormFinch_Parse()
    {
        // Each parse needs a unique URI since repository caches by URI
        _schemaCounter++;
        using var doc = System.Text.Json.JsonDocument.Parse(_schemaJson);
        var schemaUri = new Uri($"urn:benchmark:{_schemaCounter}");

        if (!_repository.TryRegisterSchema(doc.RootElement.Clone(), schemaUri, out var schemaData))
        {
            throw new InvalidOperationException("Failed to register schema");
        }

        return _factory.GetValidator(schemaData!.SchemaUri!);
    }

    [Benchmark(Description = "JsonSchema.Net")]
    public JsonSchema JsonSchemaNet_Parse()
    {
        return JsonSchema.FromText(_schemaJson);
    }

    [Benchmark(Description = "LateApex")]
    public JsonValidator LateApex_Parse()
    {
        return new JsonValidator(_schemaJson);
    }
}
