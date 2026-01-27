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
/// Cold parsing benchmarks that measure first-run/startup schema parsing costs.
/// Each iteration creates a fresh validator instance to simulate cold start.
/// </summary>
[Config(typeof(ColdRunConfig))]
[MemoryDiagnoser]
public class ColdParsingBenchmarks
{
    private string _schemaJson = null!;

    [Params(SchemaComplexity.Simple, SchemaComplexity.Medium, SchemaComplexity.Complex)]
    public SchemaComplexity Complexity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _schemaJson = BenchmarkData.GetSchema(Complexity);
    }

    [Benchmark(Baseline = true, Description = "FormFinch")]
    public object FormFinch_Parse()
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt => opt.EnableDraft202012 = true);
        using var provider = services.BuildServiceProvider();
        provider.InitializeSingletonServices();

        var repository = provider.GetRequiredService<ISchemaRepository>();
        var factory = provider.GetRequiredService<ISchemaValidatorFactory>();

        using var doc = System.Text.Json.JsonDocument.Parse(_schemaJson);
        if (!repository.TryRegisterSchema(doc.RootElement.Clone(), out var schemaData))
        {
            throw new InvalidOperationException("Failed to register schema");
        }

        return factory.GetValidator(schemaData!.SchemaUri!);
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
