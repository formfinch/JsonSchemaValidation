// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidationBenchmarks.Adapters;

public sealed class JsonSchemaValidationAdapter : IPreparsedSchemaValidatorAdapter
{
    public string Name => "JsonSchemaValidation";
    public string Runtime => "dotnet";

    private ServiceProvider? _serviceProvider;
    private ISchemaValidator? _validator;
    private IJsonValidationContextFactory? _contextFactory;

    public void PrepareSchema(string schemaJson)
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt =>
        {
            opt.EnableDraft202012 = true;
            opt.EnableDraft201909 = true;
            // Format is annotation-only by default per JSON Schema spec
            opt.Draft202012.FormatAssertionEnabled = false;
            opt.Draft201909.FormatAssertionEnabled = false;
        });
        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.InitializeSingletonServices();

        var repository = _serviceProvider.GetRequiredService<ISchemaRepository>();
        var factory = _serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        _contextFactory = _serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        // Load remote schemas for test suite compatibility
        LoadRemoteSchemas(repository);

        using var doc = JsonDocument.Parse(schemaJson);
        if (!repository.TryRegisterSchema(doc.RootElement.Clone(), out var schemaData))
        {
            throw new InvalidOperationException("Failed to register schema");
        }

        _validator = factory.GetValidator(schemaData!.SchemaUri!);
    }

    private static void LoadRemoteSchemas(ISchemaRepository repository)
    {
        // Find test suite remotes path
        var basePath = AppContext.BaseDirectory;
        var remotesPath = FindRemotesPath(basePath);
        if (remotesPath == null) return;

        // Load draft2020-12 remotes
        LoadRemotesFromPath(repository, Path.Combine(remotesPath, "draft2020-12"), "http://localhost:1234/draft2020-12/");

        // Load draft2019-09 remotes
        LoadRemotesFromPath(repository, Path.Combine(remotesPath, "draft2019-09"), "http://localhost:1234/draft2019-09/");

        // Load root-level remotes
        LoadRemotesFromPath(repository, remotesPath, "http://localhost:1234/", topLevelOnly: true);
    }

    private static string? FindRemotesPath(string basePath)
    {
        var current = basePath;
        for (int i = 0; i < 10; i++)
        {
            var remotesPath = Path.Combine(current, "submodules", "JSON-Schema-Test-Suite", "remotes");
            if (Directory.Exists(remotesPath))
                return remotesPath;

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return null;
    }

    private static void LoadRemotesFromPath(ISchemaRepository repository, string path, string baseUrl, bool topLevelOnly = false)
    {
        if (!Directory.Exists(path)) return;

        var searchOption = topLevelOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
        foreach (var file in Directory.GetFiles(path, "*.json", searchOption))
        {
            try
            {
                var content = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(content);
                var relativePath = Path.GetRelativePath(path, file).Replace("\\", "/");
                var schemaUri = new Uri(baseUrl + relativePath);
                repository.TryRegisterSchema(doc.RootElement.Clone(), schemaUri, out _);
            }
            catch
            {
                // Ignore errors loading remote schemas
            }
        }
    }

    public bool Validate(string dataJson)
    {
        using var doc = JsonDocument.Parse(dataJson);
        var context = _contextFactory!.CreateContextForRoot(doc.RootElement);
        // Use ValidateRoot (not IsValidRoot) to enable annotation tracking
        // required for unevaluatedItems/unevaluatedProperties
        return _validator!.ValidateRoot(context).IsValid;
    }

    public bool Validate(JsonElement data)
    {
        var context = _contextFactory!.CreateContextForRoot(data);
        // Use ValidateRoot (not IsValidRoot) to enable annotation tracking
        // required for unevaluatedItems/unevaluatedProperties
        return _validator!.ValidateRoot(context).IsValid;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
