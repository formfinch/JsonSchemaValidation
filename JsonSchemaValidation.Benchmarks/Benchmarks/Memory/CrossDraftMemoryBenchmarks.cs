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

namespace FormFinch.JsonSchemaValidation.Benchmarks.Benchmarks.Memory;

/// <summary>
/// Cross-draft memory allocation benchmarks to verify optimizations apply across all drafts.
/// Run at optimization milestones to ensure consistent performance across JSON Schema versions.
/// Uses a schema compatible with all drafts (common features only).
///
/// Run with: dotnet run -c Release -- --filter *CrossDraftMemory*
/// </summary>
[Config(typeof(QuickConfig))]
[MemoryDiagnoser]
public class CrossDraftMemoryBenchmarks
{
    // A schema valid in all drafts - uses only common features
    private const string CrossDraftSchema = """
        {
            "type": "object",
            "properties": {
                "id": { "type": "integer", "minimum": 1 },
                "name": { "type": "string", "minLength": 1, "maxLength": 200 },
                "email": { "type": "string", "pattern": "^[^@]+@[^@]+$" },
                "status": { "enum": ["active", "inactive", "pending"] },
                "tags": {
                    "type": "array",
                    "items": { "type": "string", "minLength": 1, "maxLength": 50 },
                    "minItems": 0,
                    "maxItems": 10
                },
                "score": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 100
                },
                "metadata": {
                    "type": "object",
                    "additionalProperties": { "type": "string" }
                }
            },
            "required": ["id", "name", "email", "status"],
            "additionalProperties": false
        }
        """;

    private const string ValidInstance = """
        {
            "id": 42,
            "name": "FormFinch Test User",
            "email": "test@formfinch.com",
            "status": "active",
            "tags": ["benchmark", "test", "validation"],
            "score": 75.5,
            "metadata": {
                "created_by": "benchmark",
                "version": "1.0"
            }
        }
        """;

    [Params("Draft4", "Draft6", "Draft7", "Draft2019-09", "Draft2020-12")]
    public string Draft { get; set; } = null!;

    private JsonElement _instanceElement;
    private ServiceProvider _formFinchProvider = null!;
    private ISchemaValidator _formFinchValidator = null!;
    private IJsonValidationContextFactory _formFinchContextFactory = null!;

    [GlobalSetup]
    public void Setup()
    {
        using var instanceDoc = JsonDocument.Parse(ValidInstance);
        _instanceElement = instanceDoc.RootElement.Clone();

        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt => EnableDraft(opt, Draft));
        _formFinchProvider = services.BuildServiceProvider();
        _formFinchProvider.InitializeSingletonServices();

        var repository = _formFinchProvider.GetRequiredService<ISchemaRepository>();
        var factory = _formFinchProvider.GetRequiredService<ISchemaValidatorFactory>();
        _formFinchContextFactory = _formFinchProvider.GetRequiredService<IJsonValidationContextFactory>();

        using var schemaDoc = JsonDocument.Parse(CrossDraftSchema);
        repository.TryRegisterSchema(schemaDoc.RootElement.Clone(), out var schemaData);
        _formFinchValidator = factory.GetValidator(schemaData!.SchemaUri!);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _formFinchProvider?.Dispose();
    }

    [Benchmark(Description = "FormFinch Dynamic")]
    public bool FormFinch_Dynamic()
    {
        var context = _formFinchContextFactory.CreateContextForRoot(_instanceElement);
        return _formFinchValidator.IsValid(context);
    }

    private static void EnableDraft(SchemaValidationOptions opt, string draft)
    {
        // Set the default draft version to match what we're testing
        // This determines which draft is used for schemas without $schema
        opt.DefaultDraftVersion = draft switch
        {
            "Draft4" => "http://json-schema.org/draft-04/schema#",
            "Draft6" => "http://json-schema.org/draft-06/schema#",
            "Draft7" => "http://json-schema.org/draft-07/schema#",
            "Draft2019-09" => "https://json-schema.org/draft/2019-09/schema",
            "Draft2020-12" => "https://json-schema.org/draft/2020-12/schema",
            _ => "https://json-schema.org/draft/2020-12/schema"
        };

        // Disable all drafts, then enable only the one we want
        opt.EnableDraft3 = false;
        opt.EnableDraft4 = draft == "Draft4";
        opt.EnableDraft6 = draft == "Draft6";
        opt.EnableDraft7 = draft == "Draft7";
        opt.EnableDraft201909 = draft == "Draft2019-09";
        opt.EnableDraft202012 = draft == "Draft2020-12";
    }
}
