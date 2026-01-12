using System.Text.Json;
using System.Text.Json.Nodes;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidationBenchmarks.Adapters;

/// <summary>
/// Adapter that forces Draft 2019-09 for all schemas, enabling fair comparison with Draft 2020-12.
/// </summary>
public sealed class JsonSchemaValidation2019Adapter : ISchemaValidatorAdapter
{
    private const string DraftUri = "https://json-schema.org/draft/2019-09/schema";

    public string Name => "JSV-2019-09";
    public string Runtime => "dotnet";

    private ServiceProvider? _serviceProvider;
    private ISchemaValidator? _validator;
    private IJsonValidationContextFactory? _contextFactory;

    public void PrepareSchema(string schemaJson)
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt =>
        {
            opt.EnableDraft202012 = false;
            opt.EnableDraft201909 = true;
            opt.FormatAssertionEnabled = true;
            opt.DefaultDraftVersion = DraftUri;
        });
        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.InitializeSingletonServices();

        var repository = _serviceProvider.GetRequiredService<ISchemaRepository>();
        var factory = _serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        _contextFactory = _serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        // Force the $schema to Draft 2019-09
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

            // Replace $ref pointing to 2020-12 meta-schema
            if (obj.TryGetPropertyValue("$ref", out var refNode) && refNode is JsonValue refValue)
            {
                var refStr = refValue.ToString();
                if (refStr.Contains("draft/2020-12"))
                {
                    obj["$ref"] = refStr.Replace("draft/2020-12", "draft/2019-09");
                }
            }

            // Convert Draft 2020-12's prefixItems + items to Draft 2019-09's items array + additionalItems
            // Draft 2020-12: prefixItems for positional, items for additional
            // Draft 2019-09: items as array for positional, additionalItems for additional
            if (obj.ContainsKey("prefixItems"))
            {
                var prefixItems = obj["prefixItems"];
                obj.Remove("prefixItems");

                // If items also exists (in 2020-12 form), move it to additionalItems
                if (obj.ContainsKey("items") && !obj.ContainsKey("additionalItems"))
                {
                    var items = obj["items"];
                    obj.Remove("items");
                    obj["additionalItems"] = items;
                }

                obj["items"] = prefixItems;
            }

            // Convert $dynamicRef/$dynamicAnchor to $recursiveRef/$recursiveAnchor
            // These are not fully equivalent but provide basic compatibility
            if (obj.ContainsKey("$dynamicAnchor"))
            {
                var anchor = obj["$dynamicAnchor"];
                obj.Remove("$dynamicAnchor");
                // $recursiveAnchor is boolean true, not a string name
                // Only set if anchor exists (2019-09 uses simpler model)
                obj["$recursiveAnchor"] = true;
            }

            if (obj.TryGetPropertyValue("$dynamicRef", out var dynRefNode) && dynRefNode is JsonValue dynRefValue)
            {
                var dynRefStr = dynRefValue.ToString();
                obj.Remove("$dynamicRef");
                // Convert to $recursiveRef (simpler model in 2019-09)
                obj["$recursiveRef"] = dynRefStr;
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
