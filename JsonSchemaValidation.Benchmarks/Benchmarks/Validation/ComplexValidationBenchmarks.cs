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

namespace FormFinch.JsonSchemaValidation.Benchmarks.Benchmarks.Validation;

/// <summary>
/// Validation benchmarks for complex schemas with logic combinators and $ref.
/// </summary>
[Config(typeof(DefaultConfig))]
[MemoryDiagnoser]
public class ComplexValidationBenchmarks
{
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

    [Params(SchemaComplexity.Medium, SchemaComplexity.Complex)]
    public SchemaComplexity Complexity { get; set; }

    [Params(true, false)]
    public bool IsValidInstance { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var schemaJson = BenchmarkData.GetSchema(Complexity);
        _instanceJson = IsValidInstance
            ? BenchmarkData.GetValidInstance(Complexity)
            : BenchmarkData.GetInvalidInstance(Complexity);

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

    [Benchmark(Baseline = true, Description = "FormFinch")]
    public bool FormFinch_Validate()
    {
        var context = _formFinchContextFactory.CreateContextForRoot(_instanceElement);
        return _formFinchValidator.ValidateRoot(context).IsValid;
    }

    [Benchmark(Description = "JsonSchema.Net")]
    public bool JsonSchemaNet_Validate()
    {
        var result = _jsonSchemaNet.Evaluate(_instanceNode, _jsonSchemaNetOptions);
        return result.IsValid;
    }

    [Benchmark(Description = "LateApex")]
    public bool LateApex_Validate()
    {
        return _lateApex.Validate(_instanceJson).IsValid;
    }
}
