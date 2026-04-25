// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

/// <summary>
/// Represents the JSON Schema draft version.
/// </summary>
public enum SchemaDraft
{
    Draft3,
    Draft4,
    Draft6,
    Draft7,
    Draft201909,
    Draft202012
}

/// <summary>
/// Describes how a schema draft was selected during draft detection.
/// </summary>
public enum DraftDetectionSource
{
    ExplicitSchemaUri,
    DefaultDraft,
    InferredSchemaUri
}

/// <summary>
/// Result of draft detection.
/// </summary>
public readonly struct DraftDetectionResult
{
    /// <summary>
    /// Whether detection was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// The detected draft (valid only when Success is true).
    /// </summary>
    public SchemaDraft Draft { get; }

    /// <summary>
    /// Error message when detection failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// The $schema URI that was found (null if not present).
    /// </summary>
    public string? SchemaUri { get; }

    /// <summary>
    /// How the detected draft was selected.
    /// </summary>
    public DraftDetectionSource Source { get; }

    private DraftDetectionResult(bool success, SchemaDraft draft, string? errorMessage, string? schemaUri, DraftDetectionSource source)
    {
        Success = success;
        Draft = draft;
        ErrorMessage = errorMessage;
        SchemaUri = schemaUri;
        Source = source;
    }

    public static DraftDetectionResult Detected(
        SchemaDraft draft,
        string? schemaUri,
        DraftDetectionSource source)
        => new(true, draft, null, schemaUri, source);

    public static DraftDetectionResult Detected(SchemaDraft draft, string schemaUri)
        => Detected(draft, schemaUri, DraftDetectionSource.ExplicitSchemaUri);

    public static DraftDetectionResult Error(string message, string? schemaUri = null)
        => new(false, default, message, schemaUri, default);
}

/// <summary>
/// Detects the JSON Schema draft version from a schema document.
/// </summary>
public static class SchemaDraftDetector
{
    private static readonly Dictionary<string, SchemaDraft> SchemaUriToVersion = new(StringComparer.OrdinalIgnoreCase)
    {
        // Draft 3
        ["http://json-schema.org/draft-03/schema"] = SchemaDraft.Draft3,
        ["http://json-schema.org/draft-03/schema#"] = SchemaDraft.Draft3,

        // Draft 4
        ["http://json-schema.org/draft-04/schema"] = SchemaDraft.Draft4,
        ["http://json-schema.org/draft-04/schema#"] = SchemaDraft.Draft4,

        // Draft 6
        ["http://json-schema.org/draft-06/schema"] = SchemaDraft.Draft6,
        ["http://json-schema.org/draft-06/schema#"] = SchemaDraft.Draft6,

        // Draft 7
        ["http://json-schema.org/draft-07/schema"] = SchemaDraft.Draft7,
        ["http://json-schema.org/draft-07/schema#"] = SchemaDraft.Draft7,

        // Draft 2019-09
        ["https://json-schema.org/draft/2019-09/schema"] = SchemaDraft.Draft201909,
        ["https://json-schema.org/draft/2019-09/schema#"] = SchemaDraft.Draft201909,

        // Draft 2020-12
        ["https://json-schema.org/draft/2020-12/schema"] = SchemaDraft.Draft202012,
        ["https://json-schema.org/draft/2020-12/schema#"] = SchemaDraft.Draft202012
    };

    /// <summary>
    /// Detects the JSON Schema draft version from a schema document.
    /// If $schema is missing or empty, defaults to Draft 2020-12 (common practice for simple schemas).
    /// Returns an error only for invalid $schema values (wrong type or unrecognized URI).
    /// </summary>
    /// <param name="schema">The root schema element.</param>
    /// <param name="defaultDraft">The draft to use when no $schema is present. If null, defaults to Draft202012.</param>
    /// <returns>Detection result with draft or error message.</returns>
    public static DraftDetectionResult DetectDraft(JsonElement schema, SchemaDraft? defaultDraft = null)
    {
        var fallbackDraft = defaultDraft ?? SchemaDraft.Draft202012;

        // Boolean schemas are valid in 2019-09 and 2020-12
        if (schema.ValueKind == JsonValueKind.True || schema.ValueKind == JsonValueKind.False)
        {
            // Boolean schemas without $schema - use fallback (defaults to Draft 2020-12)
            return DraftDetectionResult.Detected(fallbackDraft, null, DraftDetectionSource.DefaultDraft);
        }

        if (schema.ValueKind != JsonValueKind.Object)
        {
            return DraftDetectionResult.Error($"Schema root must be an object or boolean, got {schema.ValueKind}");
        }

        if (!schema.TryGetProperty("$schema", out var schemaProperty))
        {
            // Missing $schema uses fallback draft
            return DraftDetectionResult.Detected(fallbackDraft, null, DraftDetectionSource.DefaultDraft);
        }

        if (schemaProperty.ValueKind != JsonValueKind.String)
        {
            return DraftDetectionResult.Error(
                $"$schema must be a string, got {schemaProperty.ValueKind}");
        }

        var schemaUri = schemaProperty.GetString();
        if (string.IsNullOrEmpty(schemaUri))
        {
            // Empty $schema uses fallback draft
            return DraftDetectionResult.Detected(fallbackDraft, null, DraftDetectionSource.DefaultDraft);
        }

        if (SchemaUriToVersion.TryGetValue(schemaUri, out var draft))
        {
            return DraftDetectionResult.Detected(draft, schemaUri, DraftDetectionSource.ExplicitSchemaUri);
        }

        // Try to infer draft from URI patterns (for custom metaschemas that extend standard drafts)
        var inferredDraft = InferDraftFromUri(schemaUri);
        if (inferredDraft.HasValue)
        {
            return DraftDetectionResult.Detected(inferredDraft.Value, schemaUri, DraftDetectionSource.InferredSchemaUri);
        }

        return DraftDetectionResult.Error(
            $"Unrecognized $schema URI: {schemaUri}. " +
            "Supported drafts: Draft 3, Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12.",
            schemaUri);
    }

    /// <summary>
    /// Tries to infer the draft version from a custom metaschema URI by looking for draft patterns.
    /// </summary>
    private static SchemaDraft? InferDraftFromUri(string uri)
    {
        // Check for draft patterns in the URI (case-insensitive)
        var lowerUri = uri.ToLowerInvariant();

        if (lowerUri.Contains("2020-12") || lowerUri.Contains("draft2020-12"))
            return SchemaDraft.Draft202012;

        if (lowerUri.Contains("2019-09") || lowerUri.Contains("draft2019-09"))
            return SchemaDraft.Draft201909;

        if (lowerUri.Contains("draft-07") || lowerUri.Contains("draft7"))
            return SchemaDraft.Draft7;

        if (lowerUri.Contains("draft-06") || lowerUri.Contains("draft6"))
            return SchemaDraft.Draft6;

        if (lowerUri.Contains("draft-04") || lowerUri.Contains("draft4"))
            return SchemaDraft.Draft4;

        if (lowerUri.Contains("draft-03") || lowerUri.Contains("draft3"))
            return SchemaDraft.Draft3;

        return null;
    }

    /// <summary>
    /// Gets the namespace suffix for a draft version (e.g., "Draft3", "Draft202012").
    /// </summary>
    /// <param name="draft">The draft version.</param>
    /// <returns>The namespace suffix.</returns>
    public static string GetNamespace(SchemaDraft draft) => draft switch
    {
        SchemaDraft.Draft3 => "Draft3",
        SchemaDraft.Draft4 => "Draft4",
        SchemaDraft.Draft6 => "Draft6",
        SchemaDraft.Draft7 => "Draft7",
        SchemaDraft.Draft201909 => "Draft201909",
        SchemaDraft.Draft202012 => "Draft202012",
        _ => throw new ArgumentOutOfRangeException(nameof(draft), $"Unknown draft: {draft}")
    };
}
