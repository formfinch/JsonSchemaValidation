// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates ipv4 and ipv6 formats.

using System.Net;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords.Format
{
    internal sealed class IPAddressValidator : IKeywordValidator
    {
        private readonly string _formatName;
        private readonly bool _ipv6Validation;

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public IPAddressValidator(bool isIPV6Format = false)
        {
            _ipv6Validation = isIPV6Format;
            _formatName = isIPV6Format ? "ipv6" : "ipv4";
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            return str == null || IsValidIPAddress(str);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value is not a valid {_formatName} address");

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = _formatName }
            };
        }

        private bool IsValidIPAddress(string address)
        {
            if (!IPAddress.TryParse(address, out var ip))
            {
                return false;
            }

            if (_ipv6Validation)
            {
                // For IPv6 validation, check if the result was indeed an IPv6 address.
                if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                    return false;

                // Check for disallowed zone identifier.
                if (address.Contains('%'))
                    return false;
            }
            else
            {
                // Ensure that it's an IPv4 address if not validating IPv6.
                if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    return false;

                // Further validate the structure for IPv4.
                var parts = address.Split('.');
                if (parts.Length != 4 || parts.Any(part => !int.TryParse(part, System.Globalization.CultureInfo.InvariantCulture, out int _)))
                    return false;
            }

            return true;
        }
    }
}
