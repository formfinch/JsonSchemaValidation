// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords.Format;

/// <summary>
/// Provides static format validation methods for use in compiled validators.
/// Wraps the internal format validators with a public static API.
/// Draft 6 supports: date-time, email, hostname, ipv4, ipv6, uri, uri-reference, json-pointer.
/// </summary>
public static class FormatValidators
{
    private static readonly DateTimeValidator DateTimeInner = new();
    private static readonly EmailValidator EmailInner = new();
    private static readonly HostnameValidator HostnameInner = new(isIDNFormat: false);
    private static readonly IPAddressValidator Ipv4Inner = new(isIPV6Format: false);
    private static readonly IPAddressValidator Ipv6Inner = new(isIPV6Format: true);
    private static readonly UriValidator UriInner = new(iriSupport: false);
    private static readonly UriValidator UriReferenceInner = new(iriSupport: false, canBeRelative: true);
    private static readonly UriValidator UriTemplateInner = new(isTemplate: true);
    private static readonly JsonPointerValidator JsonPointerInner = new();

    /// <summary>Validates that the JSON element conforms to the "date-time" format (RFC 3339).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidDateTime(JsonElement data) => DateTimeInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "email" format (RFC 5321).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidEmail(JsonElement data) => EmailInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "hostname" format (RFC 1123).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidHostname(JsonElement data) => HostnameInner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "ipv4" format (RFC 2673).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidIpv4(JsonElement data) => Ipv4Inner.IsValid(data);

    /// <summary>Validates that the JSON element conforms to the "ipv6" format (RFC 4291).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidIpv6(JsonElement data) => Ipv6Inner.IsValid(data);

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

    /// <summary>Validates that the JSON element conforms to the "json-pointer" format (RFC 6901).</summary>
    /// <param name="data">The JSON element to validate.</param>
    /// <returns><see langword="true"/> if the element is valid or is not a string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidJsonPointer(JsonElement data) => JsonPointerInner.IsValid(data);
}
