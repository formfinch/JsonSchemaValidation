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

        var importName = ResolveImport(context, format);
        if (importName == null) return string.Empty; // annotation-only for this draft
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

        var importName = ResolveImport(context, format);
        if (importName != null) yield return importName;
    }

    /// <summary>
    /// Runtime function for a format: a built-in format supported for the draft, or, when
    /// extended formats are enabled, an extended catalog format. Built-in names win.
    /// </summary>
    private static string? ResolveImport(JsCodeGenerationContext context, string format)
    {
        if (SupportedFormatsByDraft.TryGetValue(context.DetectedDraft, out var supported) &&
            supported.Contains(format))
        {
            return MapFormatToImport(format);
        }

        return context.ExtendedFormats ? MapExtendedFormatToImport(format) : null;
    }

    /// <summary>
    /// Extended catalog formats (see FORMATS.md). Must list every catalog name; the shared
    /// format vectors fail for any name missing here.
    /// </summary>
    private static string? MapExtendedFormatToImport(string format) => format switch
    {
        "iso-13616-iban" => "isValidIso13616Iban",
        "iso-9362-bic" => "isValidIso9362Bic",
        "iso-2108-isbn" => "isValidIso2108Isbn",
        "nl-bsn" => "isValidNlBsn",
        "nl-vat" => "isValidNlVat",
        "nl-kvk" => "isValidNlKvk",
        "nl-postcode" => "isValidNlPostcode",
        "nl-phone" => "isValidNlPhone",
        "be-phone" => "isValidBePhone",
        "itu-e164-phone" => "isValidItuE164Phone",
        "be-postcode" => "isValidBePostcode",
        "nl-plate" => "isValidNlPlate",
        "gb-phone" => "isValidGbPhone",
        "gb-postcode" => "isValidGbPostcode",
        "gb-nhs" => "isValidGbNhs",
        "gb-vat" => "isValidGbVat",
        "gb-crn" => "isValidGbCrn",
        "gb-sort-code" => "isValidGbSortCode",
        "gb-account-number" => "isValidGbAccountNumber",
        "gb-nino" => "isValidGbNino",
        "gb-upn" => "isValidGbUpn",
        "gb-plate" => "isValidGbPlate",
        "de-phone" => "isValidDePhone",
        "de-postcode" => "isValidDePostcode",
        "de-vat" => "isValidDeVat",
        "de-idnr" => "isValidDeIdnr",
        "de-stnr" => "isValidDeStnr",
        "de-trade-register" => "isValidDeTradeRegister",
        "de-leitweg" => "isValidDeLeitweg",
        "de-rvnr" => "isValidDeRvnr",
        "de-kvnr" => "isValidDeKvnr",
        "de-id-card" => "isValidDeIdCard",
        "de-passport" => "isValidDePassport",
        "de-wkn" => "isValidDeWkn",
        "de-plate" => "isValidDePlate",
        "at-phone" => "isValidAtPhone",
        "at-postcode" => "isValidAtPostcode",
        "at-vat" => "isValidAtVat",
        "at-svnr" => "isValidAtSvnr",
        "at-fn" => "isValidAtFn",
        "at-tin" => "isValidAtTin",
        "at-plate" => "isValidAtPlate",
        "at-passport" => "isValidAtPassport",
        "li-phone" => "isValidLiPhone",
        "li-postcode" => "isValidLiPostcode",
        "li-peid" => "isValidLiPeid",
        "li-plate" => "isValidLiPlate",
        "ch-phone" => "isValidChPhone",
        "ch-postcode" => "isValidChPostcode",
        "ch-uid" => "isValidChUid",
        "ch-vat" => "isValidChVat",
        "ch-ahv" => "isValidChAhv",
        "ch-qr-reference" => "isValidChQrReference",
        "ch-qr-iban" => "isValidChQrIban",
        "ch-plate" => "isValidChPlate",
        "ch-passport" => "isValidChPassport",
        "lu-phone" => "isValidLuPhone",
        "lu-postcode" => "isValidLuPostcode",
        "lu-vat" => "isValidLuVat",
        "lu-matricule" => "isValidLuMatricule",
        "lu-rcs" => "isValidLuRcs",
        "lu-plate" => "isValidLuPlate",
        _ => null,
    };

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
