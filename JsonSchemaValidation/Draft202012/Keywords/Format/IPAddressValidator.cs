using System.Net;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class IPAddressValidator : IKeywordValidator
    {
        private readonly string _formatName;
        private readonly bool _ipv6Validation;

        public string Keyword => "format";

        public IPAddressValidator(bool isIPV6Format = false)
        {
            _ipv6Validation = isIPV6Format;
            _formatName = isIPV6Format ? "ipv6" : "ipv4";
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
            {
                // If the instance is not a string, it's considered valid with respect to the format keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (IsValidIPAddress(instanceString))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation) with
                {
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = _formatName }
                };
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value is not a valid {_formatName} address");
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
