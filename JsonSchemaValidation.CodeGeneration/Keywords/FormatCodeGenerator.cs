using System.Text.Json;
using FormFinch.JsonSchemaValidation.Draft202012.Keywords.Format;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "format" keyword.
/// Uses the same format validators as the dynamic validator via public singleton wrappers.
/// </summary>
public sealed class FormatCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "format";
    public int Priority => 40;

    // Known format validators that can be used in generated code
    private static readonly HashSet<string> SupportedFormats = new(StringComparer.Ordinal)
    {
        "date-time",
        "date",
        "duration",
        "email",
        "hostname",
        "idn-email",
        "idn-hostname",
        "ipv4",
        "ipv6",
        "iri",
        "iri-reference",
        "json-pointer",
        "regex",
        "relative-json-pointer",
        "time",
        "uri",
        "uri-reference",
        "uri-template",
        "uuid",
    };

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!schema.TryGetProperty("format", out var formatElement) ||
            formatElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var format = formatElement.GetString();
        return !string.IsNullOrEmpty(format) && SupportedFormats.Contains(format);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("format", out var formatElement))
        {
            return string.Empty;
        }

        var format = formatElement.GetString();
        if (string.IsNullOrEmpty(format) || !SupportedFormats.Contains(format))
        {
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
        "duration" => "FormatValidators.IsValidDuration",
        "email" => "FormatValidators.IsValidEmail",
        "hostname" => "FormatValidators.IsValidHostname",
        "idn-email" => "FormatValidators.IsValidEmail",
        "idn-hostname" => "FormatValidators.IsValidIdnHostname",
        "ipv4" => "FormatValidators.IsValidIpv4",
        "ipv6" => "FormatValidators.IsValidIpv6",
        "iri" => "FormatValidators.IsValidIri",
        "iri-reference" => "FormatValidators.IsValidIriReference",
        "json-pointer" => "FormatValidators.IsValidJsonPointer",
        "regex" => "FormatValidators.IsValidRegex",
        "relative-json-pointer" => "FormatValidators.IsValidRelativeJsonPointer",
        "time" => "FormatValidators.IsValidTime",
        "uri" => "FormatValidators.IsValidUri",
        "uri-reference" => "FormatValidators.IsValidUriReference",
        "uri-template" => "FormatValidators.IsValidUriTemplate",
        "uuid" => "FormatValidators.IsValidUuid",
        _ => throw new InvalidOperationException($"Unknown format: {format}")
    };
}
