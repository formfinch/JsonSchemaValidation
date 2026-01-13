using System.Text.Json;
using System.Text.Json.Nodes;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidationBenchmarks.Adapters;

/// <summary>
/// Adapter that forces Draft 3 for all schemas, enabling fair comparison with other draft versions.
/// </summary>
public sealed class JsonSchemaValidation3Adapter : ISchemaValidatorAdapter
{
    // Use the same URI format as the internal system (without trailing #)
    private const string DraftUri = "http://json-schema.org/draft-03/schema";

    public string Name => "JSV-Draft3";
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
            opt.EnableDraft6 = false;
            opt.EnableDraft4 = false;
            opt.EnableDraft3 = true;
            opt.FormatAssertionEnabled = true;
            opt.DefaultDraftVersion = DraftUri;
        });
        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.InitializeSingletonServices();

        var repository = _serviceProvider.GetRequiredService<ISchemaRepository>();
        var factory = _serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        _contextFactory = _serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        // Force the $schema to Draft 3
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

            // Replace $ref pointing to newer draft meta-schemas
            if (obj.TryGetPropertyValue("$ref", out var refNode) && refNode is JsonValue refValue)
            {
                var refStr = refValue.ToString();
                // Replace full meta-schema URIs to Draft 3 format
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
                else if (refStr.StartsWith("http://json-schema.org/draft-06/schema"))
                {
                    obj["$ref"] = refStr.Replace(
                        "http://json-schema.org/draft-06/schema",
                        DraftUri);
                }
                else if (refStr.StartsWith("http://json-schema.org/draft-04/schema"))
                {
                    obj["$ref"] = refStr.Replace(
                        "http://json-schema.org/draft-04/schema",
                        DraftUri);
                }
            }

            // Replace $id with id for Draft 3 compatibility
            if (obj.ContainsKey("$id") && !obj.ContainsKey("id"))
            {
                var idValue = obj["$id"];
                obj.Remove("$id");
                obj["id"] = idValue;
            }

            // Replace $defs with definitions for Draft 3 compatibility
            if (obj.ContainsKey("$defs") && !obj.ContainsKey("definitions"))
            {
                var defs = obj["$defs"];
                obj.Remove("$defs");
                obj["definitions"] = defs;
            }

            // Convert multipleOf to divisibleBy for Draft 3
            if (obj.ContainsKey("multipleOf") && !obj.ContainsKey("divisibleBy"))
            {
                var multipleOf = obj["multipleOf"];
                obj.Remove("multipleOf");
                obj["divisibleBy"] = multipleOf;
            }

            // Convert prefixItems + items to Draft 3's items array + additionalItems
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
                                var cloned = JsonNode.Parse(prop.Value.ToJsonString());
                                dependencies[prop.Key] = cloned;
                            }
                        }
                        obj.Remove("dependentRequired");
                    }
                }
            }

            // Throw for keywords that cannot be converted to Draft 3
            if (obj.ContainsKey("$dynamicRef"))
                throw new NotSupportedException("$dynamicRef is not supported in Draft 3");
            if (obj.ContainsKey("$dynamicAnchor"))
                throw new NotSupportedException("$dynamicAnchor is not supported in Draft 3");
            if (obj.ContainsKey("$recursiveRef"))
                throw new NotSupportedException("$recursiveRef is not supported in Draft 3");
            if (obj.ContainsKey("$recursiveAnchor"))
                throw new NotSupportedException("$recursiveAnchor is not supported in Draft 3");
            if (obj.ContainsKey("minContains"))
                throw new NotSupportedException("minContains is not supported in Draft 3");
            if (obj.ContainsKey("maxContains"))
                throw new NotSupportedException("maxContains is not supported in Draft 3");
            if (obj.ContainsKey("unevaluatedItems"))
                throw new NotSupportedException("unevaluatedItems is not supported in Draft 3");
            if (obj.ContainsKey("unevaluatedProperties"))
                throw new NotSupportedException("unevaluatedProperties is not supported in Draft 3");
            if (obj.ContainsKey("if"))
                throw new NotSupportedException("if/then/else is not supported in Draft 3");
            if (obj.ContainsKey("contentEncoding"))
                throw new NotSupportedException("contentEncoding is not supported in Draft 3");
            if (obj.ContainsKey("contentMediaType"))
                throw new NotSupportedException("contentMediaType is not supported in Draft 3");
            // Draft 4+ only keywords not in Draft 3
            if (obj.ContainsKey("const"))
                throw new NotSupportedException("const is not supported in Draft 3");
            if (obj.ContainsKey("contains"))
                throw new NotSupportedException("contains is not supported in Draft 3");
            if (obj.ContainsKey("propertyNames"))
                throw new NotSupportedException("propertyNames is not supported in Draft 3");
            if (obj.ContainsKey("maxProperties"))
                throw new NotSupportedException("maxProperties is not supported in Draft 3");
            if (obj.ContainsKey("minProperties"))
                throw new NotSupportedException("minProperties is not supported in Draft 3");
            // Draft 4 keywords not in Draft 3
            if (obj.ContainsKey("allOf"))
                throw new NotSupportedException("allOf is not supported in Draft 3 - use extends instead");
            if (obj.ContainsKey("anyOf"))
                throw new NotSupportedException("anyOf is not supported in Draft 3");
            if (obj.ContainsKey("oneOf"))
                throw new NotSupportedException("oneOf is not supported in Draft 3");
            if (obj.ContainsKey("not"))
                throw new NotSupportedException("not is not supported in Draft 3 - use disallow instead");

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
