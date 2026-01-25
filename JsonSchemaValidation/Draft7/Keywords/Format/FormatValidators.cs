// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords.Format;

/// <summary>
/// Provides static format validation methods for use in compiled validators.
/// Wraps the internal format validators with a public static API.
/// Draft 7 supports: date-time, date, time, email, idn-email, hostname, idn-hostname,
/// ipv4, ipv6, uri, uri-reference, iri, iri-reference, json-pointer, relative-json-pointer, regex.
/// Note: duration and uuid were added in Draft 2019-09.
/// </summary>
public static class FormatValidators
{
    private static readonly DateTimeValidator DateTimeInner = new();
    private static readonly DateValidator DateInner = new();
    private static readonly TimeValidator TimeInner = new();
    private static readonly EmailValidator EmailInner = new();
    private static readonly HostnameValidator HostnameInner = new(isIDNFormat: false);
    private static readonly HostnameValidator IdnHostnameInner = new(isIDNFormat: true);
    private static readonly IPAddressValidator Ipv4Inner = new(isIPV6Format: false);
    private static readonly IPAddressValidator Ipv6Inner = new(isIPV6Format: true);
    private static readonly UriValidator UriInner = new(iriSupport: false);
    private static readonly UriValidator UriReferenceInner = new(iriSupport: false, canBeRelative: true);
    private static readonly UriValidator IriInner = new(iriSupport: true);
    private static readonly UriValidator IriReferenceInner = new(iriSupport: true, canBeRelative: true);
    private static readonly JsonPointerValidator JsonPointerInner = new();
    private static readonly RelativeJsonPointerValidator RelativeJsonPointerInner = new();
    private static readonly RegexValidator RegexInner = new();

    public static bool IsValidDateTime(JsonElement data) => DateTimeInner.IsValid(data);
    public static bool IsValidDate(JsonElement data) => DateInner.IsValid(data);
    public static bool IsValidTime(JsonElement data) => TimeInner.IsValid(data);
    public static bool IsValidEmail(JsonElement data) => EmailInner.IsValid(data);
    public static bool IsValidHostname(JsonElement data) => HostnameInner.IsValid(data);
    public static bool IsValidIdnHostname(JsonElement data) => IdnHostnameInner.IsValid(data);
    public static bool IsValidIpv4(JsonElement data) => Ipv4Inner.IsValid(data);
    public static bool IsValidIpv6(JsonElement data) => Ipv6Inner.IsValid(data);
    public static bool IsValidUri(JsonElement data) => UriInner.IsValid(data);
    public static bool IsValidUriReference(JsonElement data) => UriReferenceInner.IsValid(data);
    public static bool IsValidIri(JsonElement data) => IriInner.IsValid(data);
    public static bool IsValidIriReference(JsonElement data) => IriReferenceInner.IsValid(data);
    public static bool IsValidJsonPointer(JsonElement data) => JsonPointerInner.IsValid(data);
    public static bool IsValidRelativeJsonPointer(JsonElement data) => RelativeJsonPointerInner.IsValid(data);
    public static bool IsValidRegex(JsonElement data) => RegexInner.IsValid(data);
}
