// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Nodes;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidationBenchmarks.Adapters;

/// <summary>
/// Adapter that forces Draft 6 for all schemas, enabling fair comparison with other draft versions.
/// </summary>
public sealed class JsonSchemaValidation6Adapter : IPreparsedSchemaValidatorAdapter
{
    // Use the same URI format as the internal system (without trailing #)
    private const string DraftUri = "http://json-schema.org/draft-06/schema";

    public string Name => "JSV-Draft6";
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
            opt.EnableDraft201909 = false;
            opt.EnableDraft7 = false;
            opt.EnableDraft6 = true;
            opt.Draft6.FormatAssertionEnabled = true;
            opt.DefaultDraftVersion = DraftUri;
        });
        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.InitializeSingletonServices();

        var repository = _serviceProvider.GetRequiredService<ISchemaRepository>();
        var factory = _serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        _contextFactory = _serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        // Force the $schema to Draft 6
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

            // Replace $ref pointing to newer draft meta-schemas
            if (obj.TryGetPropertyValue("$ref", out var refNode) && refNode is JsonValue refValue)
            {
                var refStr = refValue.ToString();
                // Replace full meta-schema URIs to Draft 6 format
                if (refStr.StartsWith("https://json-schema.org/draft/2020-12/schema"))
                {
                    obj["$ref"] = refStr.Replace(
                        "https://json-schema.org/draft/2020-12/schema",
                        DraftUri);
                }
                else if (refStr.StartsWith("https://json-schema.org/draft/2019-09/schema"))
                {
                    obj["$ref"] = refStr.Replace(
                        "https://json-schema.org/draft/2019-09/schema",
                        DraftUri);
                }
                else if (refStr.StartsWith("http://json-schema.org/draft-07/schema"))
                {
                    obj["$ref"] = refStr.Replace(
                        "http://json-schema.org/draft-07/schema",
                        DraftUri);
                }
            }

            // Replace $defs with definitions for Draft 6 compatibility
            if (obj.ContainsKey("$defs") && !obj.ContainsKey("definitions"))
            {
                var defs = obj["$defs"];
                obj.Remove("$defs");
                obj["definitions"] = defs;
            }

            // Convert prefixItems + items to Draft 6's items array + additionalItems
            // Draft 2020-12: prefixItems for positional, items for additional
            // Draft 6: items as array for positional, additionalItems for additional
            if (obj.ContainsKey("prefixItems"))
            {
                var prefixItems = obj["prefixItems"];

                // If items also exists, move it to additionalItems
                if (obj.ContainsKey("items") && !obj.ContainsKey("additionalItems"))
                {
                    var items = obj["items"];
                    obj.Remove("items");
                    obj["additionalItems"] = items;
                }

                // Move prefixItems to items
                obj.Remove("prefixItems");
                obj["items"] = prefixItems;
            }

            // Convert dependentSchemas to dependencies (schema form)
            // Draft 2020-12 split dependencies into dependentSchemas and dependentRequired
            // Draft 6 uses a single dependencies keyword
            if (obj.ContainsKey("dependentSchemas") || obj.ContainsKey("dependentRequired"))
            {
                JsonObject? dependencies = null;
                if (obj.ContainsKey("dependencies") && obj["dependencies"] is JsonObject existingDeps)
                {
                    dependencies = existingDeps;
                }
                else if (!obj.ContainsKey("dependencies"))
                {
                    dependencies = new JsonObject();
                    obj["dependencies"] = dependencies;
                }

                if (dependencies != null)
                {
                    // Merge dependentSchemas
                    if (obj.TryGetPropertyValue("dependentSchemas", out var depSchemas) && depSchemas is JsonObject depSchemasObj)
                    {
                        foreach (var prop in depSchemasObj.ToList())
                        {
                            if (prop.Value != null && !dependencies.ContainsKey(prop.Key))
                            {
                                // Clone the value to avoid parent issues
                                var cloned = JsonNode.Parse(prop.Value.ToJsonString());
                                dependencies[prop.Key] = cloned;
                            }
                        }
                        obj.Remove("dependentSchemas");
                    }

                    // Merge dependentRequired (arrays become property dependencies)
                    if (obj.TryGetPropertyValue("dependentRequired", out var depRequired) && depRequired is JsonObject depRequiredObj)
                    {
                        foreach (var prop in depRequiredObj.ToList())
                        {
                            if (prop.Value != null && !dependencies.ContainsKey(prop.Key))
                            {
                                // Clone the value to avoid parent issues
                                var cloned = JsonNode.Parse(prop.Value.ToJsonString());
                                dependencies[prop.Key] = cloned;
                            }
                        }
                        obj.Remove("dependentRequired");
                    }
                }
            }

            // Throw for 2020-12/2019-09/Draft7 only keywords that cannot be converted
            // This ensures the benchmark test is skipped rather than giving Draft 6 an unfair advantage
            if (obj.ContainsKey("$dynamicRef"))
                throw new NotSupportedException("$dynamicRef is not supported in Draft 6");
            if (obj.ContainsKey("$dynamicAnchor"))
                throw new NotSupportedException("$dynamicAnchor is not supported in Draft 6");
            if (obj.ContainsKey("$recursiveRef"))
                throw new NotSupportedException("$recursiveRef is not supported in Draft 6");
            if (obj.ContainsKey("$recursiveAnchor"))
                throw new NotSupportedException("$recursiveAnchor is not supported in Draft 6");
            if (obj.ContainsKey("minContains"))
                throw new NotSupportedException("minContains is not supported in Draft 6");
            if (obj.ContainsKey("maxContains"))
                throw new NotSupportedException("maxContains is not supported in Draft 6");
            if (obj.ContainsKey("unevaluatedItems"))
                throw new NotSupportedException("unevaluatedItems is not supported in Draft 6");
            if (obj.ContainsKey("unevaluatedProperties"))
                throw new NotSupportedException("unevaluatedProperties is not supported in Draft 6");
            if (obj.ContainsKey("if"))
                throw new NotSupportedException("if/then/else is not supported in Draft 6");
            if (obj.ContainsKey("contentEncoding"))
                throw new NotSupportedException("contentEncoding is not supported in Draft 6");
            if (obj.ContainsKey("contentMediaType"))
                throw new NotSupportedException("contentMediaType is not supported in Draft 6");

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
