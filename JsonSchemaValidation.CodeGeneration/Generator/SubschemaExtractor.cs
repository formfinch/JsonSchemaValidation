using System.Text.Json;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

/// <summary>
/// Extracts and deduplicates subschemas from a JSON Schema.
/// </summary>
public sealed class SubschemaExtractor
{
    // Keywords that require fallback to dynamic validators
    // Note: $dynamicRef is NOT here because local $dynamicRef can be resolved statically
    // Note: unevaluatedProperties/unevaluatedItems are now supported via annotation tracking
    private static readonly HashSet<string> FallbackKeywords = new(StringComparer.Ordinal)
    {
        // Currently empty - all keywords are handled by code generation
    };

    // Keywords that contain object-valued subschemas
    private static readonly HashSet<string> ObjectSubschemaKeywords = new(StringComparer.Ordinal)
    {
        "additionalProperties",
        "additionalItems",
        "items",
        "contains",
        "not",
        "if",
        "then",
        "else",
        "propertyNames",
        "unevaluatedProperties",
        "unevaluatedItems",
        "contentSchema"
    };

    // Keywords that contain object-of-subschemas (property name -> subschema)
    private static readonly HashSet<string> ObjectOfSubschemasKeywords = new(StringComparer.Ordinal)
    {
        "properties",
        "patternProperties",
        "dependentSchemas",
        "$defs",
        "definitions"
    };

    // Keywords that contain array-of-subschemas
    private static readonly HashSet<string> ArrayOfSubschemasKeywords = new(StringComparer.Ordinal)
    {
        "allOf",
        "anyOf",
        "oneOf",
        "prefixItems"
    };

    private readonly Dictionary<string, SubschemaInfo> _uniqueSchemas = new(StringComparer.Ordinal);
    private readonly HashSet<string> _visitedRefs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonElement> _anchors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonElement> _schemasByResolvedId = new(StringComparer.Ordinal);
    // Track $dynamicAnchors separately with their resource scope depth
    // List is ordered from outer (root) to inner, so first match is the "outermost" one
    private readonly List<DynamicAnchorInfo> _dynamicAnchors = new();
    private int _totalCount;
    private JsonElement _rootSchema;
    private Uri? _baseUri;
    private bool _hasUnevaluatedProperties;
    private bool _hasUnevaluatedItems;
    private int _currentResourceDepth;

    /// <summary>
    /// Extracts all unique subschemas from a root schema.
    /// </summary>
    /// <param name="rootSchema">The root schema to analyze.</param>
    /// <param name="baseUri">Optional base URI for resolving relative $id values.</param>
    /// <returns>Dictionary mapping hash to subschema info.</returns>
    public Dictionary<string, SubschemaInfo> ExtractUniqueSubschemas(JsonElement rootSchema, Uri? baseUri = null)
    {
        _uniqueSchemas.Clear();
        _visitedRefs.Clear();
        _anchors.Clear();
        _schemasByResolvedId.Clear();
        _dynamicAnchors.Clear();
        _totalCount = 0;
        _rootSchema = rootSchema;
        _baseUri = baseUri;
        _hasUnevaluatedProperties = false;
        _hasUnevaluatedItems = false;
        _currentResourceDepth = 0;

        WalkSchema(rootSchema, baseUri, jsonPointerPath: "");

        return new Dictionary<string, SubschemaInfo>(_uniqueSchemas, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the root schema that was analyzed.
    /// </summary>
    public JsonElement RootSchema => _rootSchema;

    /// <summary>
    /// Gets the total count of subschemas encountered (including duplicates).
    /// </summary>
    public int TotalSubschemaCount => _totalCount;

    /// <summary>
    /// Gets whether the schema tree contains unevaluatedProperties keyword.
    /// </summary>
    public bool HasUnevaluatedProperties => _hasUnevaluatedProperties;

    /// <summary>
    /// Gets whether the schema tree contains unevaluatedItems keyword.
    /// </summary>
    public bool HasUnevaluatedItems => _hasUnevaluatedItems;

    /// <summary>
    /// Gets the hash for a schema element.
    /// </summary>
    public string GetHash(JsonElement schema)
    {
        return SchemaHasher.ComputeHash(schema);
    }

    /// <summary>
    /// Resolves a local $ref (e.g., "#/$defs/foo", "#", or "#anchorName") to the target schema.
    /// Resolves against the root schema.
    /// </summary>
    /// <param name="refValue">The $ref value (must start with #).</param>
    /// <returns>The resolved schema, or null if not found.</returns>
    public JsonElement? ResolveLocalRef(string refValue)
    {
        return ResolveLocalRefInResource(refValue, _rootSchema);
    }

    /// <summary>
    /// Resolves a local $ref within a specific schema resource.
    /// </summary>
    /// <param name="refValue">The $ref value (must start with #).</param>
    /// <param name="resourceRoot">The schema resource to resolve within.</param>
    /// <returns>The resolved schema, or null if not found.</returns>
    public JsonElement? ResolveLocalRefInResource(string refValue, JsonElement resourceRoot)
    {
        if (string.IsNullOrEmpty(refValue) || !refValue.StartsWith('#'))
        {
            return null;
        }

        // Handle "#" - reference to resource root
        if (refValue == "#")
        {
            return resourceRoot;
        }

        // Handle JSON Pointer (e.g., "#/$defs/foo" or "#/properties/bar")
        if (refValue.StartsWith("#/"))
        {
            var pointer = refValue[1..]; // Remove the leading #
            return ResolveJsonPointer(resourceRoot, pointer);
        }

        // Handle anchor reference (e.g., "#myAnchor")
        // Search for $anchor or $dynamicAnchor within the resource root
        var anchorName = refValue[1..]; // Remove the leading #
        return ResolveAnchorInResource(anchorName, resourceRoot);
    }

    /// <summary>
    /// Resolves an anchor name within a specific schema resource.
    /// Searches for both $anchor and $dynamicAnchor.
    /// </summary>
    private JsonElement? ResolveAnchorInResource(string anchorName, JsonElement resourceRoot)
    {
        // Search the resource tree for the anchor
        return FindAnchorInSchema(anchorName, resourceRoot);
    }

    /// <summary>
    /// Recursively searches for an anchor within a schema.
    /// </summary>
    private JsonElement? FindAnchorInSchema(string anchorName, JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Check if this schema has the anchor
        if (schema.TryGetProperty("$anchor", out var anchor) &&
            anchor.ValueKind == JsonValueKind.String &&
            anchor.GetString() == anchorName)
        {
            return schema;
        }

        if (schema.TryGetProperty("$dynamicAnchor", out var dynamicAnchor) &&
            dynamicAnchor.ValueKind == JsonValueKind.String &&
            dynamicAnchor.GetString() == anchorName)
        {
            return schema;
        }

        // Search in subschema-containing keywords
        foreach (var keyword in ObjectSubschemaKeywords)
        {
            if (schema.TryGetProperty(keyword, out var subschema))
            {
                // Skip if this subschema has its own $id (different resource)
                if (subschema.ValueKind == JsonValueKind.Object &&
                    subschema.TryGetProperty("$id", out _))
                {
                    continue;
                }

                var result = FindAnchorInSchema(anchorName, subschema);
                if (result.HasValue) return result;
            }
        }

        foreach (var keyword in ObjectOfSubschemasKeywords)
        {
            if (schema.TryGetProperty(keyword, out var container) &&
                container.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in container.EnumerateObject())
                {
                    // Skip if this subschema has its own $id (different resource)
                    if (prop.Value.ValueKind == JsonValueKind.Object &&
                        prop.Value.TryGetProperty("$id", out _))
                    {
                        continue;
                    }

                    var result = FindAnchorInSchema(anchorName, prop.Value);
                    if (result.HasValue) return result;
                }
            }
        }

        foreach (var keyword in ArrayOfSubschemasKeywords)
        {
            if (schema.TryGetProperty(keyword, out var array) &&
                array.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in array.EnumerateArray())
                {
                    // Skip if this subschema has its own $id (different resource)
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("$id", out _))
                    {
                        continue;
                    }

                    var result = FindAnchorInSchema(anchorName, item);
                    if (result.HasValue) return result;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves an anchor name to the schema that declares it.
    /// </summary>
    /// <param name="anchorName">The anchor name (without the # prefix).</param>
    /// <returns>The schema with the anchor, or null if not found.</returns>
    public JsonElement? ResolveAnchor(string anchorName)
    {
        if (_anchors.TryGetValue(anchorName, out var schema))
        {
            return schema;
        }
        return null;
    }

    /// <summary>
    /// Resolves a URI to an internal schema that has a matching $id.
    /// </summary>
    /// <param name="uri">The absolute URI to look up.</param>
    /// <returns>The schema with the matching $id, or null if not found.</returns>
    public JsonElement? ResolveInternalId(string uri)
    {
        if (_schemasByResolvedId.TryGetValue(uri, out var schema))
        {
            return schema;
        }
        return null;
    }

    /// <summary>
    /// Finds the outermost $dynamicAnchor with the given name that would be in scope
    /// when validating from the root schema.
    /// </summary>
    /// <param name="anchorName">The anchor name to find.</param>
    /// <returns>The outermost (closest to root) $dynamicAnchor with that name, or null.</returns>
    public JsonElement? FindOutermostDynamicAnchor(string anchorName)
    {
        // _dynamicAnchors is ordered from outer (root) to inner
        // So the first match is the outermost one
        foreach (var anchor in _dynamicAnchors)
        {
            if (anchor.Name == anchorName)
            {
                return anchor.Schema;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if there's a $dynamicAnchor with the given name in an outer scope
    /// (at a lower depth than the specified resource depth).
    /// </summary>
    /// <param name="anchorName">The anchor name to check.</param>
    /// <param name="currentResourceDepth">The depth of the current resource.</param>
    /// <returns>The outermost $dynamicAnchor if found in an outer scope, or null.</returns>
    public JsonElement? FindOuterDynamicAnchor(string anchorName, int currentResourceDepth)
    {
        // Find the first $dynamicAnchor with this name that's at a lower depth (outer scope)
        foreach (var anchor in _dynamicAnchors)
        {
            if (anchor.Name == anchorName && anchor.ResourceDepth < currentResourceDepth)
            {
                return anchor.Schema;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the resource depth for a subschema.
    /// </summary>
    public int GetResourceDepth(string hash)
    {
        if (_uniqueSchemas.TryGetValue(hash, out var info))
        {
            return info.ResourceDepth;
        }
        return 0;
    }

    private static JsonElement? ResolveJsonPointer(JsonElement root, string pointer)
    {
        if (string.IsNullOrEmpty(pointer) || pointer == "/")
        {
            return root;
        }

        // URL-decode the pointer first (handles %25 -> %, %22 -> ", etc.)
        pointer = Uri.UnescapeDataString(pointer);

        var current = root;
        // Don't use RemoveEmptyEntries - empty string segments are valid in JSON Pointer
        var segments = pointer.Split('/');

        // Skip first empty segment (pointer starts with /)
        foreach (var segment in segments.Skip(1))
        {
            // Unescape JSON Pointer tokens (RFC 6901)
            var unescaped = segment.Replace("~1", "/").Replace("~0", "~");

            if (current.ValueKind == JsonValueKind.Object)
            {
                if (!current.TryGetProperty(unescaped, out var next))
                {
                    return null;
                }
                current = next;
            }
            else if (current.ValueKind == JsonValueKind.Array)
            {
                if (!int.TryParse(unescaped, out var index))
                {
                    return null;
                }
                var length = current.GetArrayLength();
                if (index < 0 || index >= length)
                {
                    return null;
                }
                current = current[index];
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    private void WalkSchema(JsonElement schema, Uri? currentBaseUri, JsonElement? currentResourceRoot = null, int? parentResourceDepth = null, string? jsonPointerPath = null)
    {
        _totalCount++;

        // Determine the depth for this schema
        // Schemas without $id inherit their parent resource's depth
        var schemaResourceDepth = parentResourceDepth ?? _currentResourceDepth;

        // Handle boolean schemas
        if (schema.ValueKind == JsonValueKind.True || schema.ValueKind == JsonValueKind.False)
        {
            RegisterSchema(schema, currentBaseUri, currentResourceRoot ?? _rootSchema, schemaResourceDepth, jsonPointerPath);
            return;
        }

        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        // Check for $id and update base URI and resource root if present
        var effectiveBaseUri = currentBaseUri;
        var effectiveResourceRoot = currentResourceRoot ?? _rootSchema;
        var childResourceDepth = schemaResourceDepth; // Depth for children without $id
        if (schema.TryGetProperty("$id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
        {
            var idValue = idElement.GetString();
            if (!string.IsNullOrEmpty(idValue))
            {
                // Resolve the $id against the current base URI
                Uri? resolvedId = null;
                if (Uri.TryCreate(idValue, UriKind.Absolute, out var absoluteId))
                {
                    resolvedId = absoluteId;
                }
                else if (currentBaseUri != null && Uri.TryCreate(currentBaseUri, idValue, out var relativeId))
                {
                    resolvedId = relativeId;
                }

                if (resolvedId != null)
                {
                    // Register the schema by its resolved $id (without fragment)
                    var idWithoutFragment = new Uri(resolvedId.GetLeftPart(UriPartial.Query));
                    _schemasByResolvedId.TryAdd(idWithoutFragment.AbsoluteUri, schema);
                    // Update base URI for nested schemas
                    effectiveBaseUri = idWithoutFragment;
                    // This schema becomes the new resource root for nested schemas
                    effectiveResourceRoot = schema;
                    // This schema starts a new resource, so it gets the current depth
                    // Children of this resource get depth+1 if they have their own $id
                    schemaResourceDepth = _currentResourceDepth;
                    _currentResourceDepth++;
                    childResourceDepth = schemaResourceDepth; // Children inherit this resource's depth
                }
            }
        }

        RegisterSchema(schema, effectiveBaseUri, effectiveResourceRoot, schemaResourceDepth, jsonPointerPath);

        // Walk all subschema-containing keywords
        foreach (var property in schema.EnumerateObject())
        {
            // Detect unevaluated* keywords
            if (property.Name == "unevaluatedProperties")
            {
                _hasUnevaluatedProperties = true;
            }
            else if (property.Name == "unevaluatedItems")
            {
                _hasUnevaluatedItems = true;
            }

            // Build child JSON Pointer path
            var childPath = jsonPointerPath != null ? $"{jsonPointerPath}/{EscapeJsonPointer(property.Name)}" : null;

            if (ObjectSubschemaKeywords.Contains(property.Name))
            {
                WalkSubschema(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childPath);
            }
            else if (ObjectOfSubschemasKeywords.Contains(property.Name))
            {
                WalkObjectOfSubschemas(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childPath);
            }
            else if (ArrayOfSubschemasKeywords.Contains(property.Name))
            {
                WalkArrayOfSubschemas(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childPath);
            }
            else if (property.Name == "items" && property.Value.ValueKind == JsonValueKind.Array)
            {
                // Draft 4/6/7 items as array
                WalkArrayOfSubschemas(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childPath);
            }
            else if (property.Name == "$ref" && property.Value.ValueKind == JsonValueKind.String)
            {
                // Walk into local $ref targets to ensure they're registered
                WalkRefTarget(property.Value.GetString(), effectiveBaseUri, effectiveResourceRoot);
            }
            else if (property.Name == "dependencies" && property.Value.ValueKind == JsonValueKind.Object)
            {
                // Legacy dependencies keyword - values can be arrays or schemas
                foreach (var dep in property.Value.EnumerateObject())
                {
                    // Only walk schema values (objects, true, false), not array values
                    if (dep.Value.ValueKind == JsonValueKind.Object ||
                        dep.Value.ValueKind == JsonValueKind.True ||
                        dep.Value.ValueKind == JsonValueKind.False)
                    {
                        var depPath = childPath != null ? $"{childPath}/{EscapeJsonPointer(dep.Name)}" : null;
                        WalkSubschema(dep.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, depPath);
                    }
                }
            }
        }
    }

    private static string EscapeJsonPointer(string segment)
    {
        return segment.Replace("~", "~0").Replace("/", "~1");
    }

    private void WalkRefTarget(string? refValue, Uri? currentBaseUri, JsonElement? currentResourceRoot)
    {
        if (string.IsNullOrEmpty(refValue) || !refValue.StartsWith('#'))
        {
            return; // External refs not supported
        }

        // Avoid infinite recursion for self-references
        if (!_visitedRefs.Add(refValue))
        {
            return;
        }

        var target = ResolveLocalRef(refValue);
        if (target.HasValue)
        {
            WalkSchema(target.Value, currentBaseUri, currentResourceRoot);
        }
    }

    private void WalkSubschema(JsonElement element, Uri? currentBaseUri, JsonElement? currentResourceRoot, int parentResourceDepth, string? jsonPointerPath)
    {
        if (element.ValueKind == JsonValueKind.Object ||
            element.ValueKind == JsonValueKind.True ||
            element.ValueKind == JsonValueKind.False)
        {
            WalkSchema(element, currentBaseUri, currentResourceRoot, parentResourceDepth, jsonPointerPath);
        }
    }

    private void WalkObjectOfSubschemas(JsonElement element, Uri? currentBaseUri, JsonElement? currentResourceRoot, int parentResourceDepth, string? parentPath)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            var childPath = parentPath != null ? $"{parentPath}/{EscapeJsonPointer(property.Name)}" : null;
            WalkSubschema(property.Value, currentBaseUri, currentResourceRoot, parentResourceDepth, childPath);
        }
    }

    private void WalkArrayOfSubschemas(JsonElement element, Uri? currentBaseUri, JsonElement? currentResourceRoot, int parentResourceDepth, string? parentPath)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var childPath = parentPath != null ? $"{parentPath}/{index}" : null;
            WalkSubschema(item, currentBaseUri, currentResourceRoot, parentResourceDepth, childPath);
            index++;
        }
    }

    private void RegisterSchema(JsonElement schema, Uri? effectiveBaseUri, JsonElement? resourceRoot, int resourceDepth, string? jsonPointerPath)
    {
        var hash = SchemaHasher.ComputeHash(schema);

        if (_uniqueSchemas.TryGetValue(hash, out var existingInfo))
        {
            // Schema already registered - but update the path if the new one is more canonical
            // Prefer paths under $defs as they're the ones that get externally referenced
            if (!string.IsNullOrEmpty(jsonPointerPath) &&
                jsonPointerPath.StartsWith("/$defs/") &&
                (string.IsNullOrEmpty(existingInfo.JsonPointerPath) || !existingInfo.JsonPointerPath.StartsWith("/$defs/")))
            {
                // Update with the better path - create a new SubschemaInfo with updated path
                _uniqueSchemas[hash] = new SubschemaInfo
                {
                    Hash = existingInfo.Hash,
                    Schema = existingInfo.Schema,
                    RequiresFallback = existingInfo.RequiresFallback,
                    FallbackKeywords = existingInfo.FallbackKeywords,
                    EffectiveBaseUri = existingInfo.EffectiveBaseUri,
                    ResourceRoot = existingInfo.ResourceRoot,
                    ResourceDepth = existingInfo.ResourceDepth,
                    JsonPointerPath = jsonPointerPath
                };
            }

            // Even if schema is already registered, we still need to register any anchor
            RegisterAnchor(schema, resourceDepth);
            return;
        }

        var fallbackKeywords = DetectFallbackKeywords(schema);

        _uniqueSchemas[hash] = new SubschemaInfo
        {
            Hash = hash,
            Schema = schema,
            RequiresFallback = fallbackKeywords.Count > 0,
            FallbackKeywords = fallbackKeywords,
            EffectiveBaseUri = effectiveBaseUri,
            ResourceRoot = resourceRoot,
            ResourceDepth = resourceDepth,
            JsonPointerPath = jsonPointerPath
        };

        // Register anchor if present
        RegisterAnchor(schema, resourceDepth);
    }

    private void RegisterAnchor(JsonElement schema, int resourceDepth)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        // Register $anchor
        if (schema.TryGetProperty("$anchor", out var anchorElement) &&
            anchorElement.ValueKind == JsonValueKind.String)
        {
            var anchorName = anchorElement.GetString();
            if (!string.IsNullOrEmpty(anchorName))
            {
                // Register anchor (first one wins if there are duplicates)
                _anchors.TryAdd(anchorName, schema);
            }
        }

        // Register $dynamicAnchor
        if (schema.TryGetProperty("$dynamicAnchor", out var dynamicAnchorElement) &&
            dynamicAnchorElement.ValueKind == JsonValueKind.String)
        {
            var anchorName = dynamicAnchorElement.GetString();
            if (!string.IsNullOrEmpty(anchorName))
            {
                // Register in the regular anchors map (for $ref resolution)
                _anchors.TryAdd(anchorName, schema);

                // Also register in the dynamic anchors list with depth info
                // (for $dynamicRef resolution with scope awareness)
                _dynamicAnchors.Add(new DynamicAnchorInfo
                {
                    Name = anchorName,
                    Schema = schema,
                    ResourceDepth = resourceDepth
                });
            }
        }
    }

    private static List<string> DetectFallbackKeywords(JsonElement schema)
    {
        var result = new List<string>();

        if (schema.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in schema.EnumerateObject())
        {
            if (FallbackKeywords.Contains(property.Name))
            {
                result.Add(property.Name);
            }

            // External $ref without fragment is supported via IRegistryAwareCompiledValidator
            // External $ref WITH fragment still requires fallback (subschema registration not implemented)
            if (property.Name == "$ref" && property.Value.ValueKind == JsonValueKind.String)
            {
                var refValue = property.Value.GetString();
                if (!string.IsNullOrEmpty(refValue) && !refValue.StartsWith('#'))
                {
                    // Check if it has a fragment
                    var fragmentIndex = refValue.IndexOf('#');
                    if (fragmentIndex > 0)
                    {
                        // External ref with fragment - requires fallback
                        result.Add("$ref (external with fragment)");
                    }
                }
            }
        }

        return result;
    }
}
