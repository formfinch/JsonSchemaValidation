// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Infrastructure;

/// <summary>
/// Provides access to embedded benchmark test data with optional checksum verification.
/// </summary>
public static class BenchmarkData
{
    private static readonly Assembly Assembly = typeof(BenchmarkData).Assembly;
    private static readonly Dictionary<string, string> Cache = new(StringComparer.Ordinal);
    private static readonly object CacheLock = new();

    /// <summary>
    /// Gets the schema JSON for the specified complexity level.
    /// </summary>
    public static string GetSchema(SchemaComplexity complexity)
    {
        return complexity switch
        {
            SchemaComplexity.Simple => GetResource("Schemas.simple.json"),
            SchemaComplexity.Medium => GetResource("Schemas.medium.json"),
            SchemaComplexity.Complex => GetResource("Schemas.complex.json"),
            SchemaComplexity.Production => GetResource("Schemas.production-github-workflow.json"),
            _ => throw new ArgumentOutOfRangeException(nameof(complexity))
        };
    }

    /// <summary>
    /// Gets the valid instance JSON for the specified complexity level.
    /// </summary>
    public static string GetValidInstance(SchemaComplexity complexity)
    {
        return complexity switch
        {
            SchemaComplexity.Simple => GetResource("Instances.simple-valid.json"),
            SchemaComplexity.Medium => GetResource("Instances.medium-valid.json"),
            SchemaComplexity.Complex => GetResource("Instances.complex-valid.json"),
            SchemaComplexity.Production => GetResource("Instances.production-valid.json"),
            _ => throw new ArgumentOutOfRangeException(nameof(complexity))
        };
    }

    /// <summary>
    /// Gets the invalid instance JSON for the specified complexity level.
    /// </summary>
    public static string GetInvalidInstance(SchemaComplexity complexity)
    {
        return complexity switch
        {
            SchemaComplexity.Simple => GetResource("Instances.simple-invalid.json"),
            SchemaComplexity.Medium => GetResource("Instances.medium-invalid.json"),
            SchemaComplexity.Complex => GetResource("Instances.complex-invalid.json"),
            SchemaComplexity.Production => GetResource("Instances.production-invalid.json"),
            _ => throw new ArgumentOutOfRangeException(nameof(complexity))
        };
    }

    /// <summary>
    /// Gets a parsed JsonElement for the specified schema.
    /// </summary>
    public static JsonElement GetSchemaElement(SchemaComplexity complexity)
    {
        var json = GetSchema(complexity);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Gets a parsed JsonElement for the valid instance.
    /// </summary>
    public static JsonElement GetValidInstanceElement(SchemaComplexity complexity)
    {
        var json = GetValidInstance(complexity);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Gets a parsed JsonElement for the invalid instance.
    /// </summary>
    public static JsonElement GetInvalidInstanceElement(SchemaComplexity complexity)
    {
        var json = GetInvalidInstance(complexity);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Gets a resource by name from embedded resources.
    /// </summary>
    public static string GetResource(string resourceName)
    {
        lock (CacheLock)
        {
            if (Cache.TryGetValue(resourceName, out var cached))
            {
                return cached;
            }

            var fullName = $"FormFinch.JsonSchemaValidation.Benchmarks.Data.{resourceName}";
            using var stream = Assembly.GetManifestResourceStream(fullName)
                ?? throw new InvalidOperationException($"Embedded resource not found: {fullName}");

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            Cache[resourceName] = content;
            return content;
        }
    }

    /// <summary>
    /// Gets the checksums dictionary from embedded resources.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetChecksums()
    {
        var json = GetResource("checksums.json");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new InvalidOperationException("Failed to parse checksums.json");
    }

    /// <summary>
    /// Gets all embedded resource names.
    /// </summary>
    public static IReadOnlyList<string> GetAllResourceNames()
    {
        return Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("FormFinch.JsonSchemaValidation.Benchmarks.Data.", StringComparison.Ordinal))
            .Select(n => n["FormFinch.JsonSchemaValidation.Benchmarks.Data.".Length..])
            .ToList();
    }
}
