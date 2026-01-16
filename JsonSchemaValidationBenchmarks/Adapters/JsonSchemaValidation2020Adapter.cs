using System.Text.Json;
using System.Text.Json.Nodes;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidationBenchmarks.Adapters;

/// <summary>
/// Adapter that forces Draft 2020-12 for all schemas, enabling fair comparison with Draft 2019-09.
/// </summary>
public sealed class JsonSchemaValidation2020Adapter : IPreparsedSchemaValidatorAdapter
{
    private const string DraftUri = "https://json-schema.org/draft/2020-12/schema";

    public string Name => "JSV-2020-12";
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
            opt.EnableDraft201909 = false;
            opt.Draft202012.FormatAssertionEnabled = true;
            opt.DefaultDraftVersion = DraftUri;
        });
        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.InitializeSingletonServices();

        var repository = _serviceProvider.GetRequiredService<ISchemaRepository>();
        var factory = _serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        _contextFactory = _serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        // Force the $schema to Draft 2020-12
        var modifiedSchemaJson = ForceSchemaVersion(schemaJson);

        using var doc = JsonDocument.Parse(modifiedSchemaJson);
        if (!repository.TryRegisterSchema(doc.RootElement.Clone(), out var schemaData))
        {
            throw new InvalidOperationException("Failed to register schema");
        }

        _validator = factory.GetValidator(schemaData!.SchemaUri!);
    }

    public bool Validate(string dataJson)
    {
        using var doc = JsonDocument.Parse(dataJson);
        var context = _contextFactory!.CreateContextForRoot(doc.RootElement);
        return _validator!.IsValidRoot(context);
    }

    public bool Validate(JsonElement data)
    {
        var context = _contextFactory!.CreateContextForRoot(data);
        return _validator!.IsValidRoot(context);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    private static string ForceSchemaVersion(string schemaJson)
    {
        var node = JsonNode.Parse(schemaJson);
        if (node is JsonObject obj)
        {
            ReplaceDraftReferences(obj);
            return obj.ToJsonString();
        }
        return schemaJson;
    }

    private static void ReplaceDraftReferences(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            // Replace $schema
            if (obj.ContainsKey("$schema"))
            {
                obj["$schema"] = DraftUri;
            }

            // Replace $ref pointing to 2019-09 meta-schema
            if (obj.TryGetPropertyValue("$ref", out var refNode) && refNode is JsonValue refValue)
            {
                var refStr = refValue.ToString();
                if (refStr.Contains("draft/2019-09"))
                {
                    obj["$ref"] = refStr.Replace("draft/2019-09", "draft/2020-12");
                }
            }

            // Recurse into all properties
            foreach (var prop in obj.ToList())
            {
                if (prop.Value != null)
                {
                    ReplaceDraftReferences(prop.Value);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    ReplaceDraftReferences(item);
                }
            }
        }
    }
}
