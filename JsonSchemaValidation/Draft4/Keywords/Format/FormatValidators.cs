// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Draft4.Keywords.Format;

/// <summary>
/// Provides static format validation methods for use in compiled validators.
/// Wraps the internal format validators with a public static API.
/// Draft 4 supports: date-time, email, hostname, ipv4, ipv6, uri.
/// </summary>
public static class FormatValidators
{
    private static readonly DateTimeValidator DateTimeInner = new();
    private static readonly EmailValidator EmailInner = new();
    private static readonly HostnameValidator HostnameInner = new(isIDNFormat: false);
    private static readonly IPAddressValidator Ipv4Inner = new(isIPV6Format: false);
    private static readonly IPAddressValidator Ipv6Inner = new(isIPV6Format: true);
    private static readonly UriValidator UriInner = new(iriSupport: false);

    public static bool IsValidDateTime(JsonElement data) => DateTimeInner.IsValid(data);
    public static bool IsValidEmail(JsonElement data) => EmailInner.IsValid(data);
    public static bool IsValidHostname(JsonElement data) => HostnameInner.IsValid(data);
    public static bool IsValidIpv4(JsonElement data) => Ipv4Inner.IsValid(data);
    public static bool IsValidIpv6(JsonElement data) => Ipv6Inner.IsValid(data);
    public static bool IsValidUri(JsonElement data) => UriInner.IsValid(data);
}
