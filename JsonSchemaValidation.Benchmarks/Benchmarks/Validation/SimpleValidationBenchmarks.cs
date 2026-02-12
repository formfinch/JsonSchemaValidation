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
/// Validation benchmarks for simple schemas with minimal overhead.
/// </summary>
[Config(typeof(DefaultConfig))]
[MemoryDiagnoser]
public class SimpleValidationBenchmarks
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

    [Params(true, false)]
    public bool IsValidInstance { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var schemaJson = BenchmarkData.GetSchema(SchemaComplexity.Simple);
        _instanceJson = IsValidInstance
            ? BenchmarkData.GetValidInstance(SchemaComplexity.Simple)
            : BenchmarkData.GetInvalidInstance(SchemaComplexity.Simple);

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
            OutputFormat = Json.Schema.OutputFormat.Flag,
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
        return _formFinchValidator.IsValid(context);
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

    [Benchmark(Description = "FormFinch (string)")]
    public bool FormFinch_ValidateString()
    {
        using var doc = JsonDocument.Parse(_instanceJson);
        var context = _formFinchContextFactory.CreateContextForRoot(doc.RootElement);
        return _formFinchValidator.IsValid(context);
    }

    [Benchmark(Description = "JsonSchema.Net (string)")]
    public bool JsonSchemaNet_ValidateString()
    {
        var node = JsonNode.Parse(_instanceJson);
        var result = _jsonSchemaNet.Evaluate(node, _jsonSchemaNetOptions);
        return result.IsValid;
    }
}
