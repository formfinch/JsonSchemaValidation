// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords.Format;

/// <summary>
/// Provides static format validation methods for use in compiled validators.
/// Wraps the internal format validators with a public static API.
/// </summary>
public static class FormatValidators
{
    private static readonly DateTimeValidator DateTimeInner = new();
    private static readonly DateValidator DateInner = new();
    private static readonly DurationValidator DurationInner = new();
    private static readonly EmailValidator EmailInner = new();
    private static readonly HostnameValidator HostnameInner = new(isIDNFormat: false);
    private static readonly HostnameValidator IdnHostnameInner = new(isIDNFormat: true);
    private static readonly IPAddressValidator Ipv4Inner = new(isIPV6Format: false);
    private static readonly IPAddressValidator Ipv6Inner = new(isIPV6Format: true);
    private static readonly UriValidator IriInner = new(iriSupport: true);
    private static readonly UriValidator IriReferenceInner = new(iriSupport: true, canBeRelative: true);
    private static readonly JsonPointerValidator JsonPointerInner = new();
    private static readonly RegexValidator RegexInner = new();
    private static readonly RelativeJsonPointerValidator RelativeJsonPointerInner = new();
    private static readonly TimeValidator TimeInner = new();
    private static readonly UriValidator UriInner = new(iriSupport: false);
    private static readonly UriValidator UriReferenceInner = new(iriSupport: false, canBeRelative: true);
    private static readonly UriValidator UriTemplateInner = new(isTemplate: true);
    private static readonly UuidValidator UuidInner = new();

    /// <summary>Validates that the JSON element conforms to the "date-time" format (RFC 3339).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidDateTime(JsonElement data) => DateTimeInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "date" format (RFC 3339 full-date).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidDate(JsonElement data) => DateInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "duration" format (ISO 8601).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidDuration(JsonElement data) => DurationInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "email" format (RFC 5321).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidEmail(JsonElement data) => EmailInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "hostname" format (RFC 1123).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidHostname(JsonElement data) => HostnameInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "idn-hostname" format (RFC 5890).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidIdnHostname(JsonElement data) => IdnHostnameInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "ipv4" format (RFC 2673).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidIpv4(JsonElement data) => Ipv4Inner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "ipv6" format (RFC 4291).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidIpv6(JsonElement data) => Ipv6Inner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "iri" format (RFC 3987).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidIri(JsonElement data) => IriInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "iri-reference" format (RFC 3987).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidIriReference(JsonElement data) => IriReferenceInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "json-pointer" format (RFC 6901).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidJsonPointer(JsonElement data) => JsonPointerInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "regex" format (ECMA-262).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidRegex(JsonElement data) => RegexInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "relative-json-pointer" format (relative JSON Pointer).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidRelativeJsonPointer(JsonElement data) => RelativeJsonPointerInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "time" format (RFC 3339 full-time).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidTime(JsonElement data) => TimeInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "uri" format (RFC 3986).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidUri(JsonElement data) => UriInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "uri-reference" format (RFC 3986).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidUriReference(JsonElement data) => UriReferenceInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "uri-template" format (RFC 6570).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidUriTemplate(JsonElement data) => UriTemplateInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "uuid" format (RFC 4122).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidUuid(JsonElement data) => UuidInner.IsValid(data);
}
