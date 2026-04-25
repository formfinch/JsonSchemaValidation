// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for the "format" keyword.
/// Emits eager validation for legacy drafts that treated format as asserting.
/// For Draft 2020-12, format stays annotation-only unless assertion is enabled.
/// </summary>
public sealed class JsFormatCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "format";
    public int Priority => 40;

    private static readonly Dictionary<SchemaDraft, HashSet<string>> SupportedFormatsByDraft = new()
    {
        [SchemaDraft.Draft4] = new(StringComparer.Ordinal)
        {
            "date-time", "email", "hostname", "ipv4", "ipv6", "uri",
        },
        [SchemaDraft.Draft201909] = new(StringComparer.Ordinal)
        {
            "date-time", "date", "time",
            "email", "idn-email",
            "hostname", "idn-hostname",
            "ipv4", "ipv6",
            "uri", "uri-reference", "uri-template",
            "iri", "iri-reference",
            "json-pointer", "relative-json-pointer",
            "regex", "uuid",
        },
        [SchemaDraft.Draft202012] = new(StringComparer.Ordinal)
        {
            "date-time", "date", "time", "duration",
            "email", "idn-email",
            "hostname", "idn-hostname",
            "ipv4", "ipv6",
            "uri", "uri-reference", "uri-template",
            "iri", "iri-reference",
            "json-pointer", "relative-json-pointer",
            "regex", "uuid",
        },
    };

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return false;
        if (!schema.TryGetProperty("format", out var f)) return false;
        return f.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(f.GetString());
    }

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!ShouldAssertFormat(context)) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("format", out var formatElem)) return string.Empty;
        var format = formatElem.GetString();
        if (string.IsNullOrEmpty(format)) return string.Empty;

        if (!SupportedFormatsByDraft.TryGetValue(context.DetectedDraft, out var supported) ||
            !supported.Contains(format))
        {
            return string.Empty; // annotation-only for this draft
        }

        var importName = MapFormatToImport(format);
        if (importName == null) return string.Empty;
        var v = context.ElementExpr;
        return $"if (!{importName}({v})) return false;";
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context)
    {
        if (!ShouldAssertFormat(context)) yield break;
        if (!context.CurrentSchema.TryGetProperty("format", out var formatElem) ||
            formatElem.ValueKind != JsonValueKind.String)
        {
            yield break;
        }
        var format = formatElem.GetString();
        if (string.IsNullOrEmpty(format)) yield break;

        if (!SupportedFormatsByDraft.TryGetValue(context.DetectedDraft, out var supported) ||
            !supported.Contains(format))
        {
            yield break;
        }
        var importName = MapFormatToImport(format);
        if (importName != null) yield return importName;
    }

    private static bool ShouldAssertFormat(JsCodeGenerationContext context)
    {
        return context.DetectedDraft switch
        {
            SchemaDraft.Draft202012 => context.FormatAssertionEnabled,
            _ => true,
        };
    }

    private static string? MapFormatToImport(string format) => format switch
    {
        "date-time" => "isValidDateTime",
        "date" => "isValidDate",
        "time" => "isValidTime",
        "duration" => "isValidDuration",
        "email" => "isValidEmail",
        "idn-email" => "isValidIdnEmail",
        "hostname" => "isValidHostname",
        "idn-hostname" => "isValidIdnHostname",
        "ipv4" => "isValidIpv4",
        "ipv6" => "isValidIpv6",
        "uri" => "isValidUri",
        "uri-reference" => "isValidUriReference",
        "uri-template" => "isValidUriTemplate",
        "iri" => "isValidIri",
        "iri-reference" => "isValidIriReference",
        "json-pointer" => "isValidJsonPointer",
        "relative-json-pointer" => "isValidRelativeJsonPointer",
        "regex" => "isValidRegex",
        "uuid" => "isValidUuid",
        _ => null,
    };
}
