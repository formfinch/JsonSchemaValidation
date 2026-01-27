// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Benchmarks.Config;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Benchmarks.CrossDraft;

/// <summary>
/// Cross-draft benchmarks measuring the same schema validated across different JSON Schema drafts.
/// Uses a schema that is valid in all drafts (Draft 4, 6, 7, 2019-09, 2020-12).
/// </summary>
[Config(typeof(DefaultConfig))]
[MemoryDiagnoser]
public class CrossDraftBenchmarks
{
    // A schema valid in all drafts
    private const string CrossDraftSchema = """
        {
            "type": "object",
            "properties": {
                "name": { "type": "string", "minLength": 1, "maxLength": 100 },
                "age": { "type": "integer", "minimum": 0, "maximum": 150 },
                "email": { "type": "string" }
            },
            "required": ["name"]
        }
        """;

    private const string ValidInstance = """
        {
            "name": "John Doe",
            "age": 30,
            "email": "john@example.com"
        }
        """;

    private JsonElement _instanceElement;

    // FormFinch validators for each draft
    private ServiceProvider _provider202012 = null!;
    private ISchemaValidator _validator202012 = null!;
    private IJsonValidationContextFactory _contextFactory202012 = null!;

    private ServiceProvider _provider201909 = null!;
    private ISchemaValidator _validator201909 = null!;
    private IJsonValidationContextFactory _contextFactory201909 = null!;

    private ServiceProvider _providerDraft7 = null!;
    private ISchemaValidator _validatorDraft7 = null!;
    private IJsonValidationContextFactory _contextFactoryDraft7 = null!;

    private ServiceProvider _providerDraft6 = null!;
    private ISchemaValidator _validatorDraft6 = null!;
    private IJsonValidationContextFactory _contextFactoryDraft6 = null!;

    private ServiceProvider _providerDraft4 = null!;
    private ISchemaValidator _validatorDraft4 = null!;
    private IJsonValidationContextFactory _contextFactoryDraft4 = null!;

    [GlobalSetup]
    public void Setup()
    {
        using var doc = JsonDocument.Parse(ValidInstance);
        _instanceElement = doc.RootElement.Clone();

        // Setup Draft 2020-12
        (_provider202012, _validator202012, _contextFactory202012) = SetupValidator(opt =>
        {
            opt.EnableDraft202012 = true;
        });

        // Setup Draft 2019-09
        (_provider201909, _validator201909, _contextFactory201909) = SetupValidator(opt =>
        {
            opt.EnableDraft201909 = true;
        });

        // Setup Draft 7
        (_providerDraft7, _validatorDraft7, _contextFactoryDraft7) = SetupValidator(opt =>
        {
            opt.EnableDraft7 = true;
        });

        // Setup Draft 6
        (_providerDraft6, _validatorDraft6, _contextFactoryDraft6) = SetupValidator(opt =>
        {
            opt.EnableDraft6 = true;
        });

        // Setup Draft 4
        (_providerDraft4, _validatorDraft4, _contextFactoryDraft4) = SetupValidator(opt =>
        {
            opt.EnableDraft4 = true;
        });
    }

    private static (ServiceProvider, ISchemaValidator, IJsonValidationContextFactory) SetupValidator(
        Action<SchemaValidationOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(configure);
        var provider = services.BuildServiceProvider();
        provider.InitializeSingletonServices();

        var repository = provider.GetRequiredService<ISchemaRepository>();
        var factory = provider.GetRequiredService<ISchemaValidatorFactory>();
        var contextFactory = provider.GetRequiredService<IJsonValidationContextFactory>();

        using var schemaDoc = JsonDocument.Parse(CrossDraftSchema);
        repository.TryRegisterSchema(schemaDoc.RootElement.Clone(), out var schemaData);
        var validator = factory.GetValidator(schemaData!.SchemaUri!);

        return (provider, validator, contextFactory);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _provider202012?.Dispose();
        _provider201909?.Dispose();
        _providerDraft7?.Dispose();
        _providerDraft6?.Dispose();
        _providerDraft4?.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Draft 2020-12")]
    public bool Draft202012_Validate()
    {
        var context = _contextFactory202012.CreateContextForRoot(_instanceElement);
        return _validator202012.ValidateRoot(context).IsValid;
    }

    [Benchmark(Description = "Draft 2019-09")]
    public bool Draft201909_Validate()
    {
        var context = _contextFactory201909.CreateContextForRoot(_instanceElement);
        return _validator201909.ValidateRoot(context).IsValid;
    }

    [Benchmark(Description = "Draft 7")]
    public bool Draft7_Validate()
    {
        var context = _contextFactoryDraft7.CreateContextForRoot(_instanceElement);
        return _validatorDraft7.ValidateRoot(context).IsValid;
    }

    [Benchmark(Description = "Draft 6")]
    public bool Draft6_Validate()
    {
        var context = _contextFactoryDraft6.CreateContextForRoot(_instanceElement);
        return _validatorDraft6.ValidateRoot(context).IsValid;
    }

    [Benchmark(Description = "Draft 4")]
    public bool Draft4_Validate()
    {
        var context = _contextFactoryDraft4.CreateContextForRoot(_instanceElement);
        return _validatorDraft4.ValidateRoot(context).IsValid;
    }
}
