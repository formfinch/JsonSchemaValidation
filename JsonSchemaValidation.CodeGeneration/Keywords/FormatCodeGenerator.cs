// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "format" keyword.
/// Uses the same format validators as the dynamic validator via public singleton wrappers.
/// Supports draft-specific formats - unsupported formats for a draft are treated as annotations (per spec).
/// </summary>
public sealed class FormatCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "format";
    public int Priority => 40;

    /// <summary>
    /// Formats supported by each draft version.
    /// Unsupported formats are treated as annotations (no validation, per JSON Schema spec).
    /// </summary>
    private static readonly Dictionary<SchemaDraft, HashSet<string>> SupportedFormatsByDraft = new()
    {
        [SchemaDraft.Draft3] = new(StringComparer.Ordinal)
        {
            "date-time", "date", "time", "email",
            "hostname", "host-name",    // Draft 3 uses "host-name", modern uses "hostname"
            "ipv4", "ip-address",       // Draft 3 uses "ip-address", modern uses "ipv4"
            "ipv6", "uri", "regex", "color"
        },
        [SchemaDraft.Draft4] = new(StringComparer.Ordinal)
        {
            "date-time", "email", "hostname", "ipv4", "ipv6", "uri"
        },
        [SchemaDraft.Draft6] = new(StringComparer.Ordinal)
        {
            "date-time", "email", "hostname", "ipv4", "ipv6", "uri", "uri-reference", "uri-template", "json-pointer"
        },
        [SchemaDraft.Draft7] = new(StringComparer.Ordinal)
        {
            "date-time", "date", "time", "email", "idn-email", "hostname", "idn-hostname",
            "ipv4", "ipv6", "uri", "uri-reference", "uri-template", "iri", "iri-reference",
            "json-pointer", "relative-json-pointer", "regex"
        },
        [SchemaDraft.Draft201909] = new(StringComparer.Ordinal)
        {
            "date-time", "date", "time", "duration", "email", "idn-email", "hostname", "idn-hostname",
            "ipv4", "ipv6", "uri", "uri-reference", "uri-template", "iri", "iri-reference",
            "json-pointer", "relative-json-pointer", "regex", "uuid"
        },
        [SchemaDraft.Draft202012] = new(StringComparer.Ordinal)
        {
            "date-time", "date", "time", "duration", "email", "idn-email", "hostname", "idn-hostname",
            "ipv4", "ipv6", "uri", "uri-reference", "uri-template", "iri", "iri-reference",
            "json-pointer", "relative-json-pointer", "regex", "uuid"
        }
    };

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // Return true for any schema with a format property - draft-specific filtering happens in GenerateCode
        return schema.TryGetProperty("format", out var formatElement) &&
               formatElement.ValueKind == JsonValueKind.String &&
               !string.IsNullOrEmpty(formatElement.GetString());
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("format", out var formatElement))
        {
            return string.Empty;
        }

        var format = formatElement.GetString();
        if (string.IsNullOrEmpty(format))
        {
            return string.Empty;
        }

        // Check if format is supported for the detected draft
        if (!SupportedFormatsByDraft.TryGetValue(context.DetectedDraft, out var supportedFormats) ||
            !supportedFormats.Contains(format))
        {
            // Format not supported for this draft - treat as annotation only (no validation)
            return string.Empty;
        }

        var e = context.ElementVariable;
        var validatorMethod = GetValidatorMethod(format);

        return $$"""
if (!{{validatorMethod}}({{e}})) return false;
""";
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        // Format validators use static singleton instances, no static fields needed
        yield break;
    }

    private static string GetValidatorMethod(string format) => format switch
    {
        "date-time" => "FormatValidators.IsValidDateTime",
        "date" => "FormatValidators.IsValidDate",
        "time" => "FormatValidators.IsValidTime",
        "duration" => "FormatValidators.IsValidDuration",
        "email" => "FormatValidators.IsValidEmail",
        "idn-email" => "FormatValidators.IsValidEmail",
        "hostname" or "host-name" => "FormatValidators.IsValidHostname",  // Draft 3 alias
        "idn-hostname" => "FormatValidators.IsValidIdnHostname",
        "ipv4" or "ip-address" => "FormatValidators.IsValidIpv4",         // Draft 3 alias
        "ipv6" => "FormatValidators.IsValidIpv6",
        "uri" => "FormatValidators.IsValidUri",
        "uri-reference" => "FormatValidators.IsValidUriReference",
        "uri-template" => "FormatValidators.IsValidUriTemplate",
        "iri" => "FormatValidators.IsValidIri",
        "iri-reference" => "FormatValidators.IsValidIriReference",
        "json-pointer" => "FormatValidators.IsValidJsonPointer",
        "relative-json-pointer" => "FormatValidators.IsValidRelativeJsonPointer",
        "regex" => "FormatValidators.IsValidRegex",
        "uuid" => "FormatValidators.IsValidUuid",
        "color" => "FormatValidators.IsValidColor",
        _ => throw new InvalidOperationException($"Unknown format: {format}")
    };
}
