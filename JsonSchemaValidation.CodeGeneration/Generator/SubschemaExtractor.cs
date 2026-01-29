// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
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
    // Note: "items" is NOT here because it can be an array (tuple validation) in Draft 3-7
    // and needs special handling in WalkSchema
    private static readonly HashSet<string> ObjectSubschemaKeywords = new(StringComparer.Ordinal)
    {
        "additionalProperties",
        "additionalItems",
        // "items" is handled specially - can be object or array depending on draft
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

    // Keywords whose values are definitely NOT schemas (metadata, validation constraints, etc.)
    // Used to identify unknown keywords that might contain schemas for $ref resolution
    private static readonly HashSet<string> NonSchemaValueKeywords = new(StringComparer.Ordinal)
    {
        // Core/identifier keywords
        "$schema",
        "$id",
        "id",
        "$ref",
        "$anchor",
        "$dynamicAnchor",
        "$dynamicRef",
        "$recursiveAnchor",
        "$recursiveRef",
        "$vocabulary",
        "$comment",
        // Metadata keywords
        "title",
        "description",
        "default",
        "deprecated",
        "readOnly",
        "writeOnly",
        // Validation keywords (non-schema values)
        "type",
        "enum",
        "const",
        "format",
        "pattern",
        "minLength",
        "maxLength",
        "minimum",
        "maximum",
        "exclusiveMinimum",
        "exclusiveMaximum",
        "multipleOf",
        "divisibleBy",
        "minItems",
        "maxItems",
        "uniqueItems",
        "minContains",
        "maxContains",
        "required",
        "minProperties",
        "maxProperties",
        "dependentRequired",
        // Content keywords
        "contentMediaType",
        "contentEncoding"
    };

    private readonly Dictionary<string, SubschemaInfo> _uniqueSchemas = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonElement> _anchors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonElement> _schemasByResolvedId = new(StringComparer.Ordinal);
    // Track $dynamicAnchors separately with their resource scope depth
    // List is ordered from outer (root) to inner, so first match is the "outermost" one
    private readonly List<DynamicAnchorInfo> _dynamicAnchors = new();
    // Track all $dynamicAnchors per resource root (keyed by resource root hash)
    // Used to populate ResourceAnchors for resource root schemas
    private readonly Dictionary<string, List<(string AnchorName, string SchemaHash)>> _resourceAnchors = new(StringComparer.Ordinal);
    // Track resource root hashes (for marking IsResourceRoot)
    private readonly HashSet<string> _resourceRootHashes = new(StringComparer.Ordinal);
    private int _totalCount;
    private JsonElement _rootSchema;
    private string _rootSchemaHash = "";
    private Uri? _baseUri;
    private bool _hasUnevaluatedProperties;
    private bool _hasUnevaluatedItems;
    private int _currentResourceDepth;
    private SchemaDraft _detectedDraft;

    /// <summary>
    /// Extracts all unique subschemas from a root schema.
    /// </summary>
    /// <param name="rootSchema">The root schema to analyze.</param>
    /// <param name="baseUri">Optional base URI for resolving relative $id values.</param>
    /// <param name="defaultDraft">Default draft to use when schema has no $schema declaration.</param>
    /// <returns>Dictionary mapping hash to subschema info.</returns>
    public Dictionary<string, SubschemaInfo> ExtractUniqueSubschemas(JsonElement rootSchema, Uri? baseUri = null, SchemaDraft? defaultDraft = null)
    {
        _uniqueSchemas.Clear();
        _anchors.Clear();
        _schemasByResolvedId.Clear();
        _dynamicAnchors.Clear();
        _resourceAnchors.Clear();
        _resourceRootHashes.Clear();
        _totalCount = 0;
        _rootSchema = rootSchema;
        _rootSchemaHash = SchemaHasher.ComputeHash(rootSchema);
        _baseUri = baseUri;
        _hasUnevaluatedProperties = false;
        _hasUnevaluatedItems = false;
        _currentResourceDepth = 0;

        // Detect draft for $ref override semantics
        var draftResult = SchemaDraftDetector.DetectDraft(rootSchema, defaultDraft);
        _detectedDraft = draftResult.Success ? draftResult.Draft : (defaultDraft ?? SchemaDraft.Draft202012);

        // Register root schema as a resource root
        _resourceRootHashes.Add(_rootSchemaHash);
        _resourceAnchors[_rootSchemaHash] = new List<(string, string)>();

        WalkSchema(rootSchema, baseUri, currentResourceRootHash: _rootSchemaHash, jsonPointerPath: "");

        // Post-process: update resource root SubschemaInfos with their ResourceAnchors
        PopulateResourceAnchors();

        return new Dictionary<string, SubschemaInfo>(_uniqueSchemas, StringComparer.Ordinal);
    }

    /// <summary>
    /// Updates resource root SubschemaInfos with all anchors found within their resource.
    /// </summary>
    private void PopulateResourceAnchors()
    {
        foreach (var resourceRootHash in _resourceRootHashes)
        {
            if (!_uniqueSchemas.TryGetValue(resourceRootHash, out var info))
            {
                continue;
            }

            var resourceAnchors = _resourceAnchors.TryGetValue(resourceRootHash, out var anchors)
                ? anchors
                : new List<(string, string)>();

            // Create updated SubschemaInfo with ResourceAnchors populated
            _uniqueSchemas[resourceRootHash] = new SubschemaInfo
            {
                Hash = info.Hash,
                Schema = info.Schema,
                RequiresFallback = info.RequiresFallback,
                FallbackKeywords = info.FallbackKeywords,
                EffectiveBaseUri = info.EffectiveBaseUri,
                ResourceRoot = info.ResourceRoot,
                ResourceRootHash = info.ResourceRootHash,
                ResourceDepth = info.ResourceDepth,
                JsonPointerPath = info.JsonPointerPath,
                DynamicAnchors = info.DynamicAnchors,
                HasRecursiveAnchor = info.HasRecursiveAnchor,
                IsResourceRoot = true,
                ResourceAnchors = resourceAnchors
            };
        }
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

        // Draft 7 and earlier: $id with fragment-only value acts as anchor (e.g., $id: "#foo")
        if (schema.TryGetProperty("$id", out var idElement) &&
            idElement.ValueKind == JsonValueKind.String)
        {
            var idValue = idElement.GetString();
            if (idValue != null && idValue.StartsWith('#') && idValue.Length > 1)
            {
                var idAnchorName = idValue[1..]; // Remove the leading #
                if (idAnchorName == anchorName)
                {
                    return schema;
                }
            }
        }

        // Draft 4 and earlier: "id" (without $) with fragment-only value acts as anchor
        if (schema.TryGetProperty("id", out var legacyIdElement) &&
            legacyIdElement.ValueKind == JsonValueKind.String)
        {
            var idValue = legacyIdElement.GetString();
            if (idValue != null && idValue.StartsWith('#') && idValue.Length > 1)
            {
                var idAnchorName = idValue[1..]; // Remove the leading #
                if (idAnchorName == anchorName)
                {
                    return schema;
                }
            }
        }

        // Search in subschema-containing keywords
        foreach (var keyword in ObjectSubschemaKeywords)
        {
            if (schema.TryGetProperty(keyword, out var subschema))
            {
                // Skip if this subschema has its own $id that creates a new resource (different resource)
                // Fragment-only $id (e.g., "#foo") doesn't create a new resource - it's just an anchor
                if (subschema.ValueKind == JsonValueKind.Object &&
                    subschema.TryGetProperty("$id", out var subId) &&
                    subId.ValueKind == JsonValueKind.String)
                {
                    var subIdValue = subId.GetString();
                    if (subIdValue != null && !subIdValue.StartsWith('#'))
                    {
                        continue; // This $id creates a new resource, skip it
                    }
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
                    // Skip if this subschema has its own $id that creates a new resource
                    // Fragment-only $id (e.g., "#foo") doesn't create a new resource
                    if (prop.Value.ValueKind == JsonValueKind.Object &&
                        prop.Value.TryGetProperty("$id", out var propId) &&
                        propId.ValueKind == JsonValueKind.String)
                    {
                        var propIdValue = propId.GetString();
                        if (propIdValue != null && !propIdValue.StartsWith('#'))
                        {
                            continue; // This $id creates a new resource, skip it
                        }
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
                    // Skip if this subschema has its own $id that creates a new resource
                    // Fragment-only $id (e.g., "#foo") doesn't create a new resource
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("$id", out var itemId) &&
                        itemId.ValueKind == JsonValueKind.String)
                    {
                        var itemIdValue = itemId.GetString();
                        if (itemIdValue != null && !itemIdValue.StartsWith('#'))
                        {
                            continue; // This $id creates a new resource, skip it
                        }
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

    /// <summary>
    /// Gets all $dynamicAnchor declarations at the root resource level (depth 0).
    /// Used for generating IScopedCompiledValidator.DynamicAnchors property.
    /// </summary>
    /// <returns>List of anchor names and their schema hashes at depth 0.</returns>
    [Obsolete("Use GetRootResourceAnchors() instead for proper resource-level anchor collection")]
    public IReadOnlyList<(string Name, string Hash)> GetRootDynamicAnchors()
    {
        var result = new List<(string, string)>();
        foreach (var anchor in _dynamicAnchors)
        {
            if (anchor.ResourceDepth == 0)
            {
                var hash = SchemaHasher.ComputeHash(anchor.Schema);
                result.Add((anchor.Name, hash));
            }
        }
        return result;
    }

    /// <summary>
    /// Gets all $dynamicAnchor declarations within the root schema resource.
    /// This includes anchors in nested subschemas (e.g., $defs) that belong to the root resource.
    /// Used for generating IScopedCompiledValidator.DynamicAnchors property.
    /// </summary>
    /// <returns>List of anchor names and their schema hashes within the root resource.</returns>
    public IReadOnlyList<(string AnchorName, string SchemaHash)> GetRootResourceAnchors()
    {
        if (_resourceAnchors.TryGetValue(_rootSchemaHash, out var anchors))
        {
            return anchors;
        }
        return [];
    }

    /// <summary>
    /// Checks if the root schema has $recursiveAnchor: true (Draft 2019-09).
    /// Used for generating IScopedCompiledValidator.HasRecursiveAnchor property.
    /// </summary>
    public bool HasRootRecursiveAnchor()
    {
        if (_rootSchema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return _rootSchema.TryGetProperty("$recursiveAnchor", out var anchor) &&
               anchor.ValueKind == JsonValueKind.True;
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

    private void WalkSchema(JsonElement schema, Uri? currentBaseUri, JsonElement? currentResourceRoot = null, int? parentResourceDepth = null, string? currentResourceRootHash = null, string? jsonPointerPath = null, bool insideUnknownKeyword = false)
    {
        _totalCount++;

        // Determine the depth for this schema
        // Schemas without $id inherit their parent resource's depth
        var schemaResourceDepth = parentResourceDepth ?? _currentResourceDepth;

        // Track which resource this schema belongs to
        var effectiveResourceRootHash = currentResourceRootHash ?? _rootSchemaHash;

        // Handle boolean schemas
        if (schema.ValueKind == JsonValueKind.True || schema.ValueKind == JsonValueKind.False)
        {
            RegisterSchema(schema, currentBaseUri, currentResourceRoot ?? _rootSchema, schemaResourceDepth, effectiveResourceRootHash, jsonPointerPath);
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
        var childResourceRootHash = effectiveResourceRootHash; // Children inherit this resource root

        // Draft 7 and earlier: $ref masks sibling keywords including $id
        // If $ref is present, sibling $id should NOT change the base URI
        var hasRef = schema.TryGetProperty("$ref", out _);
        var refMasksSiblingId = hasRef && _detectedDraft <= SchemaDraft.Draft7;

        // Per JSON Schema spec, $id inside an unknown keyword is NOT a real identifier
        // When walking inside unknown keywords, we only register by JSON Pointer path
        if (!insideUnknownKeyword && schema.TryGetProperty("$id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
        {
            var idValue = idElement.GetString();
            if (!string.IsNullOrEmpty(idValue))
            {
                // Fragment-only $id (e.g., "#foo") is a location-independent anchor in Draft 7 and earlier
                // It does NOT change the base URI - only creates an anchor reference point
                // Note: Fragment-only $id is still valid even with sibling $ref (it's an anchor, not a base URI)
                if (idValue.StartsWith('#'))
                {
                    // This is an anchor, not a base URI change
                    // The anchor is handled by FindAnchorInSchema when resolving $ref: "#foo"
                    // Don't modify effectiveBaseUri or effectiveResourceRoot
                }
                else if (refMasksSiblingId)
                {
                    // In Draft 7 and earlier, $ref masks sibling $id
                    // Don't update base URI, but still register the schema by its $id for external refs
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
                        var idWithoutFragment = new Uri(resolvedId.GetLeftPart(UriPartial.Query));
                        _schemasByResolvedId.TryAdd(idWithoutFragment.AbsoluteUri, schema);
                    }
                    // Don't modify effectiveBaseUri or effectiveResourceRoot
                }
                else
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

                        // Register this schema as a resource root with its own anchor collection
                        var schemaHash = SchemaHasher.ComputeHash(schema);
                        _resourceRootHashes.Add(schemaHash);
                        if (!_resourceAnchors.ContainsKey(schemaHash))
                        {
                            _resourceAnchors[schemaHash] = new List<(string, string)>();
                        }
                        // Children belong to this resource
                        childResourceRootHash = schemaHash;
                        // But this schema itself belongs to its own resource (for anchor tracking)
                        effectiveResourceRootHash = schemaHash;
                    }
                }
            }
        }

        // Also check for legacy "id" (without $) for Draft 4 and earlier
        // Per JSON Schema spec, id inside an unknown keyword is NOT a real identifier
        if (!insideUnknownKeyword && schema.TryGetProperty("id", out var legacyIdElement) && legacyIdElement.ValueKind == JsonValueKind.String)
        {
            var idValue = legacyIdElement.GetString();
            if (!string.IsNullOrEmpty(idValue))
            {
                // Fragment-only id (e.g., "#foo") is a location-independent anchor
                // It does NOT change the base URI - only creates an anchor reference point
                if (idValue.StartsWith('#'))
                {
                    // This is an anchor, not a base URI change
                    // The anchor is handled by FindAnchorInSchema when resolving $ref: "#foo"
                    // Don't modify effectiveBaseUri or effectiveResourceRoot
                }
                else if (refMasksSiblingId)
                {
                    // In Draft 7 and earlier, $ref masks sibling id
                    // Don't update base URI, but still register the schema by its id for external refs
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
                        var idWithoutFragment = new Uri(resolvedId.GetLeftPart(UriPartial.Query));
                        _schemasByResolvedId.TryAdd(idWithoutFragment.AbsoluteUri, schema);
                    }
                    // Don't modify effectiveBaseUri or effectiveResourceRoot
                }
                else
                {
                    // Resolve the id against the current base URI
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
                        // Register the schema by its resolved id (without fragment)
                        var idWithoutFragment = new Uri(resolvedId.GetLeftPart(UriPartial.Query));
                        _schemasByResolvedId.TryAdd(idWithoutFragment.AbsoluteUri, schema);
                        // Update base URI for nested schemas
                        effectiveBaseUri = idWithoutFragment;
                        // This schema becomes the new resource root for nested schemas
                        effectiveResourceRoot = schema;
                        // This schema starts a new resource, so it gets the current depth
                        // Children of this resource get depth+1 if they have their own id
                        schemaResourceDepth = _currentResourceDepth;
                        _currentResourceDepth++;
                        childResourceDepth = schemaResourceDepth; // Children inherit this resource's depth

                        // Register this schema as a resource root with its own anchor collection
                        var schemaHash = SchemaHasher.ComputeHash(schema);
                        _resourceRootHashes.Add(schemaHash);
                        if (!_resourceAnchors.ContainsKey(schemaHash))
                        {
                            _resourceAnchors[schemaHash] = new List<(string, string)>();
                        }
                        // Children belong to this resource
                        childResourceRootHash = schemaHash;
                        // But this schema itself belongs to its own resource (for anchor tracking)
                        effectiveResourceRootHash = schemaHash;
                    }
                }
            }
        }

        RegisterSchema(schema, effectiveBaseUri, effectiveResourceRoot, schemaResourceDepth, effectiveResourceRootHash, jsonPointerPath);

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

            // Handle "items" specially - can be object (schema) or array (tuple) depending on draft
            if (property.Name == "items")
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    // Draft 3-7, 2019-09: items as array (tuple validation)
                    WalkArrayOfSubschemas(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
                }
                else
                {
                    // items as schema (applies to all items)
                    WalkSubschema(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
                }
            }
            // Handle "extends" specially (Draft 3) - can be object (single schema) or array of schemas
            else if (property.Name == "extends")
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    WalkArrayOfSubschemas(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
                }
                else
                {
                    WalkSubschema(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
                }
            }
            // Handle "disallow" specially (Draft 3) - can contain types (strings) or schemas
            else if (property.Name == "disallow")
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    // Walk only schema elements, not type strings
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object ||
                            item.ValueKind == JsonValueKind.True ||
                            item.ValueKind == JsonValueKind.False)
                        {
                            WalkSubschema(item, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
                        }
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.Object ||
                         property.Value.ValueKind == JsonValueKind.True ||
                         property.Value.ValueKind == JsonValueKind.False)
                {
                    WalkSubschema(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
                }
            }
            // Handle "type" specially (Draft 3) - array can contain schemas
            else if (property.Name == "type" && property.Value.ValueKind == JsonValueKind.Array)
            {
                // Walk only schema elements, not type strings
                foreach (var item in property.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object ||
                        item.ValueKind == JsonValueKind.True ||
                        item.ValueKind == JsonValueKind.False)
                    {
                        WalkSubschema(item, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
                    }
                }
            }
            else if (ObjectSubschemaKeywords.Contains(property.Name))
            {
                WalkSubschema(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
            }
            else if (ObjectOfSubschemasKeywords.Contains(property.Name))
            {
                WalkObjectOfSubschemas(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
            }
            else if (ArrayOfSubschemasKeywords.Contains(property.Name))
            {
                WalkArrayOfSubschemas(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath);
            }
            // Note: We no longer call WalkRefTarget for local $refs.
            // The normal tree walk already visits all subschemas (in definitions, $defs, etc.)
            // with correct base URI propagation through intermediate $id changes.
            // WalkRefTarget was causing issues when a JSON Pointer ref (e.g., #/definitions/baz/definitions/bar)
            // skipped over intermediate schemas with $id, resulting in the wrong base URI being used.
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
                        WalkSubschema(dep.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, depPath);
                    }
                }
            }
            else if (!NonSchemaValueKeywords.Contains(property.Name) &&
                     !ObjectSubschemaKeywords.Contains(property.Name) &&
                     !ObjectOfSubschemasKeywords.Contains(property.Name) &&
                     !ArrayOfSubschemasKeywords.Contains(property.Name))
            {
                // Unknown keyword - register as potential $ref target if it looks like a schema
                // This enables $ref to arbitrary keywords like "#/unknown-keyword" or "#/examples/0"
                // Pass insideUnknownKeyword=true so that $id values inside are NOT treated as real identifiers
                if (property.Value.ValueKind == JsonValueKind.Object ||
                    property.Value.ValueKind == JsonValueKind.True ||
                    property.Value.ValueKind == JsonValueKind.False)
                {
                    WalkSubschema(property.Value, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, childPath, insideUnknownKeyword: true);
                }
                else if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    // Walk array elements that look like schemas (e.g., examples: [{...}])
                    var index = 0;
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object ||
                            item.ValueKind == JsonValueKind.True ||
                            item.ValueKind == JsonValueKind.False)
                        {
                            var itemPath = childPath != null ? $"{childPath}/{index}" : null;
                            WalkSubschema(item, effectiveBaseUri, effectiveResourceRoot, childResourceDepth, childResourceRootHash, itemPath, insideUnknownKeyword: true);
                        }
                        index++;
                    }
                }
            }
        }
    }

    private static string EscapeJsonPointer(string segment)
    {
        return segment.Replace("~", "~0").Replace("/", "~1");
    }

    private void WalkSubschema(JsonElement element, Uri? currentBaseUri, JsonElement? currentResourceRoot, int parentResourceDepth, string? currentResourceRootHash, string? jsonPointerPath, bool insideUnknownKeyword = false)
    {
        if (element.ValueKind == JsonValueKind.Object ||
            element.ValueKind == JsonValueKind.True ||
            element.ValueKind == JsonValueKind.False)
        {
            WalkSchema(element, currentBaseUri, currentResourceRoot, parentResourceDepth, currentResourceRootHash, jsonPointerPath, insideUnknownKeyword);
        }
    }

    private void WalkObjectOfSubschemas(JsonElement element, Uri? currentBaseUri, JsonElement? currentResourceRoot, int parentResourceDepth, string? currentResourceRootHash, string? parentPath)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            var childPath = parentPath != null ? $"{parentPath}/{EscapeJsonPointer(property.Name)}" : null;
            WalkSubschema(property.Value, currentBaseUri, currentResourceRoot, parentResourceDepth, currentResourceRootHash, childPath);
        }
    }

    private void WalkArrayOfSubschemas(JsonElement element, Uri? currentBaseUri, JsonElement? currentResourceRoot, int parentResourceDepth, string? currentResourceRootHash, string? parentPath)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var childPath = parentPath != null ? $"{parentPath}/{index}" : null;
            WalkSubschema(item, currentBaseUri, currentResourceRoot, parentResourceDepth, currentResourceRootHash, childPath);
            index++;
        }
    }

    private void RegisterSchema(JsonElement schema, Uri? effectiveBaseUri, JsonElement? resourceRoot, int resourceDepth, string resourceRootHash, string? jsonPointerPath)
    {
        var hash = SchemaHasher.ComputeHash(schema);

        if (_uniqueSchemas.TryGetValue(hash, out var existingInfo))
        {
            // Schema already registered - but update the path if the new one is more canonical
            // Prefer paths under $defs (2019-09+) or definitions (Draft 3-7) as they're externally referenced
            var isCanonicalPath = !string.IsNullOrEmpty(jsonPointerPath) &&
                (jsonPointerPath.StartsWith("/$defs/") || jsonPointerPath.StartsWith("/definitions/"));
            var existingIsCanonical = !string.IsNullOrEmpty(existingInfo.JsonPointerPath) &&
                (existingInfo.JsonPointerPath.StartsWith("/$defs/") || existingInfo.JsonPointerPath.StartsWith("/definitions/"));
            if (isCanonicalPath && !existingIsCanonical)
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
                    ResourceRootHash = existingInfo.ResourceRootHash,
                    ResourceDepth = existingInfo.ResourceDepth,
                    JsonPointerPath = jsonPointerPath,
                    DynamicAnchors = existingInfo.DynamicAnchors,
                    HasRecursiveAnchor = existingInfo.HasRecursiveAnchor,
                    IsResourceRoot = existingInfo.IsResourceRoot,
                    ResourceAnchors = existingInfo.ResourceAnchors
                };
            }

            // Even if schema is already registered, we still need to register any anchor
            RegisterAnchor(schema, hash, resourceDepth, resourceRootHash);
            return;
        }

        var fallbackKeywords = DetectFallbackKeywords(schema);
        var dynamicAnchors = ExtractDynamicAnchors(schema);
        var hasRecursiveAnchor = HasRecursiveAnchor(schema);

        _uniqueSchemas[hash] = new SubschemaInfo
        {
            Hash = hash,
            Schema = schema,
            RequiresFallback = fallbackKeywords.Count > 0,
            FallbackKeywords = fallbackKeywords,
            EffectiveBaseUri = effectiveBaseUri,
            ResourceRoot = resourceRoot,
            ResourceRootHash = resourceRootHash,
            ResourceDepth = resourceDepth,
            JsonPointerPath = jsonPointerPath,
            DynamicAnchors = dynamicAnchors,
            HasRecursiveAnchor = hasRecursiveAnchor,
            // IsResourceRoot and ResourceAnchors will be populated in post-processing
            IsResourceRoot = false,
            ResourceAnchors = []
        };

        // Register anchor if present
        RegisterAnchor(schema, hash, resourceDepth, resourceRootHash);
    }

    private void RegisterAnchor(JsonElement schema, string schemaHash, int resourceDepth, string resourceRootHash)
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

                // Track this anchor as belonging to its resource root
                // This enables resource-level anchor collection for $dynamicRef
                if (_resourceAnchors.TryGetValue(resourceRootHash, out var anchors))
                {
                    // Avoid duplicates (same anchor name in same resource)
                    if (!anchors.Any(a => a.AnchorName == anchorName))
                    {
                        anchors.Add((anchorName, schemaHash));
                    }
                }
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

    /// <summary>
    /// Extracts $dynamicAnchor names from a schema.
    /// </summary>
    private static List<string> ExtractDynamicAnchors(JsonElement schema)
    {
        var result = new List<string>();

        if (schema.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        if (schema.TryGetProperty("$dynamicAnchor", out var dynamicAnchor) &&
            dynamicAnchor.ValueKind == JsonValueKind.String)
        {
            var name = dynamicAnchor.GetString();
            if (!string.IsNullOrEmpty(name))
            {
                result.Add(name);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a schema has $recursiveAnchor: true.
    /// </summary>
    private static bool HasRecursiveAnchor(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (schema.TryGetProperty("$recursiveAnchor", out var recursiveAnchor) &&
            recursiveAnchor.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        return false;
    }
}
