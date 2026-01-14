using System.Text.Json;

namespace JsonSchemaValidation.CodeGenerator.CodeGenerator;

/// <summary>
/// Extracts and deduplicates subschemas from a JSON Schema.
/// </summary>
public sealed class SubschemaExtractor
{
    // Keywords that require fallback to dynamic validators
    private static readonly HashSet<string> FallbackKeywords = new(StringComparer.Ordinal)
    {
        "unevaluatedProperties",
        "unevaluatedItems",
        "$dynamicRef"
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

    private readonly SchemaHasher _hasher = new();
    private readonly Dictionary<string, SubschemaInfo> _uniqueSchemas = new(StringComparer.Ordinal);
    private int _totalCount;

    /// <summary>
    /// Extracts all unique subschemas from a root schema.
    /// </summary>
    /// <param name="rootSchema">The root schema to analyze.</param>
    /// <returns>Dictionary mapping hash to subschema info.</returns>
    public Dictionary<string, SubschemaInfo> ExtractUniqueSubschemas(JsonElement rootSchema)
    {
        _uniqueSchemas.Clear();
        _totalCount = 0;

        WalkSchema(rootSchema);

        return new Dictionary<string, SubschemaInfo>(_uniqueSchemas, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the total count of subschemas encountered (including duplicates).
    /// </summary>
    public int TotalSubschemaCount => _totalCount;

    /// <summary>
    /// Gets the hash for a schema element.
    /// </summary>
    public string GetHash(JsonElement schema)
    {
        return _hasher.ComputeHash(schema);
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
        var hash = _hasher.ComputeHash(schema);

        if (_uniqueSchemas.ContainsKey(hash))
        {
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

            // Check for external $ref
            if (property.Name == "$ref" && property.Value.ValueKind == JsonValueKind.String)
            {
                var refValue = property.Value.GetString();
                if (!string.IsNullOrEmpty(refValue) && !refValue.StartsWith('#'))
                {
                    result.Add("$ref (external)");
                }
            }
        }

        return result;
    }
}
