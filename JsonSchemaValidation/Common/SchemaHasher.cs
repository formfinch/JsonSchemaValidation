// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Common;

/// <summary>
/// Computes content-based hashes for JSON schemas to identify unique schemas.
/// Used for hash-based lookup of compiled validators.
/// </summary>
public sealed class SchemaHasher
{
    /// <summary>
    /// Shared instance for convenience.
    /// </summary>
    public static SchemaHasher Instance { get; } = new();

    // Keywords that don't affect validation behavior and should be ignored for hashing.
    //
    // $id is intentionally excluded: it affects base-URI resolution for output locations
    // and external references TO this schema, but not the validation results themselves.
    // Trade-off: schemas differing only by $id will share a cached validator.
    //
    // $schema is NOT excluded: it determines which draft's semantics apply, which directly
    // affects validation behavior.
    //
    // Note: $defs and definitions ARE included because their contents affect validation
    // when referenced via $ref.
    private static readonly HashSet<string> MetadataKeywords = new(StringComparer.Ordinal)
    {
        "$id",
        "$comment",
        "title",
        "description",
        "default",
        "examples",
        "deprecated",
        "readOnly",
        "writeOnly"
    };

    /// <summary>
    /// Computes a stable hash for a schema based on its normalized content.
    /// Ignores metadata keywords that don't affect validation.
    /// </summary>
    /// <param name="schema">The schema to hash.</param>
    /// <returns>A 12-character lowercase hex hash.</returns>
    public static string ComputeHash(JsonElement schema)
    {
        var normalized = NormalizeForHashing(schema);
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private static string NormalizeForHashing(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => NormalizeObject(element),
            JsonValueKind.Array => NormalizeArray(element),
            JsonValueKind.String => JsonSerializer.Serialize(element.GetString()),
            JsonValueKind.Number => NormalizeNumber(element),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => string.Empty
        };
    }

    private static string NormalizeObject(JsonElement element)
    {
        var properties = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in element.EnumerateObject())
        {
            // Skip metadata keywords
            if (MetadataKeywords.Contains(property.Name))
            {
                continue;
            }

            properties[property.Name] = NormalizeForHashing(property.Value);
        }

        if (properties.Count == 0)
        {
            return "{}";
        }

        var sb = new StringBuilder();
        sb.Append('{');
        var first = true;
        foreach (var (key, value) in properties)
        {
            if (!first)
            {
                sb.Append(',');
            }
            first = false;
            sb.Append('"');
            sb.Append(key);
            sb.Append("\":");
            sb.Append(value);
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static string NormalizeArray(JsonElement element)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        var first = true;
        foreach (var item in element.EnumerateArray())
        {
            if (!first)
            {
                sb.Append(',');
            }
            first = false;
            sb.Append(NormalizeForHashing(item));
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static string NormalizeNumber(JsonElement element)
    {
        // Normalize numbers to remove trailing zeros and decimal points
        if (element.TryGetInt64(out var longValue))
        {
            return longValue.ToString(CultureInfo.InvariantCulture);
        }
        if (element.TryGetDouble(out var doubleValue))
        {
            // Check if it's actually an integer
            if (Math.Abs(doubleValue - Math.Truncate(doubleValue)) < double.Epsilon)
            {
                return ((long)doubleValue).ToString(CultureInfo.InvariantCulture);
            }
            return doubleValue.ToString("G17", CultureInfo.InvariantCulture);
        }
        return element.GetRawText();
    }
}
