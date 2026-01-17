using System.Text.Json;
using JsonSchemaValidation.Common;

namespace JsonSchemaValidation.CodeGeneration.Generator;

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
    private int _totalCount;
    private JsonElement _rootSchema;
    private bool _hasUnevaluatedProperties;
    private bool _hasUnevaluatedItems;

    /// <summary>
    /// Extracts all unique subschemas from a root schema.
    /// </summary>
    /// <param name="rootSchema">The root schema to analyze.</param>
    /// <returns>Dictionary mapping hash to subschema info.</returns>
    public Dictionary<string, SubschemaInfo> ExtractUniqueSubschemas(JsonElement rootSchema)
    {
        _uniqueSchemas.Clear();
        _visitedRefs.Clear();
        _anchors.Clear();
        _totalCount = 0;
        _rootSchema = rootSchema;
        _hasUnevaluatedProperties = false;
        _hasUnevaluatedItems = false;

        WalkSchema(rootSchema);

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
    /// </summary>
    /// <param name="refValue">The $ref value (must start with #).</param>
    /// <returns>The resolved schema, or null if not found.</returns>
    public JsonElement? ResolveLocalRef(string refValue)
    {
        if (string.IsNullOrEmpty(refValue) || !refValue.StartsWith('#'))
        {
            return null;
        }

        // Handle "#" - reference to root
        if (refValue == "#")
        {
            return _rootSchema;
        }

        // Handle JSON Pointer (e.g., "#/$defs/foo" or "#/properties/bar")
        if (refValue.StartsWith("#/"))
        {
            var pointer = refValue[1..]; // Remove the leading #
            return ResolveJsonPointer(_rootSchema, pointer);
        }

        // Handle anchor reference (e.g., "#myAnchor")
        var anchorName = refValue[1..]; // Remove the leading #
        return ResolveAnchor(anchorName);
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

    private void WalkSchema(JsonElement schema)
    {
        _totalCount++;

        // Handle boolean schemas
        if (schema.ValueKind == JsonValueKind.True || schema.ValueKind == JsonValueKind.False)
        {
            RegisterSchema(schema);
            return;
        }

        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        RegisterSchema(schema);

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

            if (ObjectSubschemaKeywords.Contains(property.Name))
            {
                WalkSubschema(property.Value);
            }
            else if (ObjectOfSubschemasKeywords.Contains(property.Name))
            {
                WalkObjectOfSubschemas(property.Value);
            }
            else if (ArrayOfSubschemasKeywords.Contains(property.Name))
            {
                WalkArrayOfSubschemas(property.Value);
            }
            else if (property.Name == "items" && property.Value.ValueKind == JsonValueKind.Array)
            {
                // Draft 4/6/7 items as array
                WalkArrayOfSubschemas(property.Value);
            }
            else if (property.Name == "$ref" && property.Value.ValueKind == JsonValueKind.String)
            {
                // Walk into local $ref targets to ensure they're registered
                WalkRefTarget(property.Value.GetString());
            }
        }
    }

    private void WalkRefTarget(string? refValue)
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
            WalkSchema(target.Value);
        }
    }

    private void WalkSubschema(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object ||
            element.ValueKind == JsonValueKind.True ||
            element.ValueKind == JsonValueKind.False)
        {
            WalkSchema(element);
        }
    }

    private void WalkObjectOfSubschemas(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            WalkSubschema(property.Value);
        }
    }

    private void WalkArrayOfSubschemas(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in element.EnumerateArray())
        {
            WalkSubschema(item);
        }
    }

    private void RegisterSchema(JsonElement schema)
    {
        var hash = SchemaHasher.ComputeHash(schema);

        if (_uniqueSchemas.ContainsKey(hash))
        {
            // Even if schema is already registered, we still need to register any anchor
            RegisterAnchor(schema);
            return;
        }

        var fallbackKeywords = DetectFallbackKeywords(schema);

        _uniqueSchemas[hash] = new SubschemaInfo
        {
            Hash = hash,
            Schema = schema,
            RequiresFallback = fallbackKeywords.Count > 0,
            FallbackKeywords = fallbackKeywords
        };

        // Register anchor if present
        RegisterAnchor(schema);
    }

    private void RegisterAnchor(JsonElement schema)
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

        // Also register $dynamicAnchor - when accessed via $ref (not $dynamicRef),
        // it resolves statically just like a regular anchor
        if (schema.TryGetProperty("$dynamicAnchor", out var dynamicAnchorElement) &&
            dynamicAnchorElement.ValueKind == JsonValueKind.String)
        {
            var anchorName = dynamicAnchorElement.GetString();
            if (!string.IsNullOrEmpty(anchorName))
            {
                // Register anchor (first one wins if there are duplicates)
                _anchors.TryAdd(anchorName, schema);
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
