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
using FormFinch.JsonSchemaValidation.Compiler;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using Json.Schema;
using LateApexEarlySpeed.Json.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Benchmarks.Memory;

/// <summary>
/// Memory allocation benchmarks for quick iteration during optimization work.
/// Shows how allocations scale with schema complexity (Simple → Medium → Complex).
///
/// Run with: dotnet run -c Release -- --filter *MemoryAllocation*
/// </summary>
[Config(typeof(QuickConfig))]
[MemoryDiagnoser]
public class MemoryAllocationBenchmarks
{
    [Params(SchemaComplexity.Simple, SchemaComplexity.Medium, SchemaComplexity.Complex)]
    public SchemaComplexity Complexity { get; set; }

    private JsonElement _instanceElement;
    private JsonNode? _instanceNode;
    private string _instanceJson = null!;

    // FormFinch Dynamic
    private ServiceProvider _formFinchProvider = null!;
    private ISchemaValidator _formFinchValidator = null!;
    private IJsonValidationContextFactory _formFinchContextFactory = null!;

    // FormFinch Compiled
    private CompiledValidatorRegistry _compiledRegistry = null!;
    private RuntimeValidatorFactory _runtimeFactory = null!;
    private ICompiledValidator _formFinchCompiled = null!;

    // JsonSchema.Net
    private JsonSchema _jsonSchemaNet = null!;
    private EvaluationOptions _jsonSchemaNetOptions = null!;

    // LateApex
    private JsonValidator _lateApex = null!;

    [GlobalSetup]
    public void Setup()
    {
        var schemaJson = BenchmarkData.GetSchema(Complexity);
        _instanceJson = BenchmarkData.GetValidInstance(Complexity);

        using var doc = JsonDocument.Parse(_instanceJson);
        _instanceElement = doc.RootElement.Clone();
        _instanceNode = JsonNode.Parse(_instanceJson);

        // Setup FormFinch Dynamic
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

        // Setup FormFinch Compiled
        _compiledRegistry = new CompiledValidatorRegistry();
        _runtimeFactory = new RuntimeValidatorFactory(_compiledRegistry);
        _formFinchCompiled = _runtimeFactory.Compile(schemaJson);

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
        _runtimeFactory?.Dispose();
    }

    [Benchmark(Baseline = true, Description = "FormFinch (dynamic)")]
    public bool FormFinch_Dynamic()
    {
        var context = _formFinchContextFactory.CreateContextForRoot(_instanceElement);
        return _formFinchValidator.IsValid(context);
    }

    [Benchmark(Description = "FormFinch (compiled)")]
    public bool FormFinch_Compiled()
    {
        return _formFinchCompiled.IsValid(_instanceElement);
    }

    [Benchmark(Description = "JsonSchema.Net")]
    public bool JsonSchemaNet()
    {
        var result = _jsonSchemaNet.Evaluate(_instanceNode, _jsonSchemaNetOptions);
        return result.IsValid;
    }

    [Benchmark(Description = "LateApex")]
    public bool LateApex()
    {
        return _lateApex.Validate(_instanceJson).IsValid;
    }
}
