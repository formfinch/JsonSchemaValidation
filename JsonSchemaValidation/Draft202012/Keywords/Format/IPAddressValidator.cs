using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Net;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class IPAddressValidator : IKeywordValidator
    {
        private readonly string keyword;
        private readonly bool ipv6Validation;

        public IPAddressValidator(bool isIPV6Format = false)
        {
            ipv6Validation = isIPV6Format;
            keyword = isIPV6Format ? "format:ipv6" : "format:ipv4";
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.String)
            {
                // If the instance is not a string, it's considered valid with respect to the format keyword
                return ValidationResult.Ok;
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Ok; // This is a fallback; ideally, a JSON string should not be null.
            }

            if (IsValidIPAddress(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        private bool IsValidIPAddress(string address)
        {
            if (!IPAddress.TryParse(address, out var ip))
            {
                return false;
            }

            if (ipv6Validation)
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
                if (parts.Length != 4 || parts.Any(part => !int.TryParse(part, out int _)))
                    return false;
            }

            return true;
        }
    }
}
