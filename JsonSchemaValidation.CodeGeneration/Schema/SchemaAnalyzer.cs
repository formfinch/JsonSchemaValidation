// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

/// <summary>
/// Analyzes a JSON Schema for compilation compatibility.
/// </summary>
public sealed class SchemaAnalyzer
{
    /// <summary>
    /// Analyzes a schema file.
    /// </summary>
    /// <param name="schemaPath">Path to the schema file.</param>
    /// <returns>Analysis result.</returns>
    public AnalysisResult Analyze(string schemaPath)
    {
        var json = File.ReadAllText(schemaPath);
        using var doc = JsonDocument.Parse(json);
        return Analyze(doc.RootElement);
    }

    /// <summary>
    /// Analyzes a schema element.
    /// </summary>
    /// <param name="schema">The schema to analyze.</param>
    /// <returns>Analysis result.</returns>
    public AnalysisResult Analyze(JsonElement schema)
    {
        var extractor = new SubschemaExtractor();
        var uniqueSchemas = extractor.ExtractUniqueSubschemas(schema);

        var fallbackKeywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var subschema in uniqueSchemas.Values)
        {
            foreach (var keyword in subschema.FallbackKeywords)
            {
                fallbackKeywords.Add(keyword);
            }
        }

        return new AnalysisResult
        {
            TotalSubschemas = extractor.TotalSubschemaCount,
            UniqueSubschemas = uniqueSchemas.Count,
            FullyInlinable = fallbackKeywords.Count == 0,
            FallbackKeywords = fallbackKeywords.OrderBy(k => k).ToList()
        };
    }
}

/// <summary>
/// Result of schema analysis.
/// </summary>
public sealed class AnalysisResult
{
    /// <summary>
    /// Total number of subschemas encountered (including duplicates).
    /// </summary>
    public int TotalSubschemas { get; init; }

    /// <summary>
    /// Number of unique subschemas (after deduplication).
    /// </summary>
    public int UniqueSubschemas { get; init; }

    /// <summary>
    /// Whether the schema can be fully inlined without fallbacks.
    /// </summary>
    public bool FullyInlinable { get; init; }

    /// <summary>
    /// Keywords that require fallback to dynamic validators.
    /// </summary>
    public IReadOnlyList<string> FallbackKeywords { get; init; } = [];
}
