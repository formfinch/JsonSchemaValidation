using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class HostnameValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);
        private static readonly IdnMapping idn = new IdnMapping();

        // Simplified regex pattern for ASCII hostname validation
        private static readonly string asciiHostnamePattern = @"^(([a-z0-9]|[a-z0-9][a-z0-9\-]*[a-z0-9])\.)*([a-z0-9]|[a-z0-9][a-z0-9\-]*[a-z0-9])$";

        private readonly Regex hostnameRegex;
        private readonly bool performIDNConversion;
        private readonly string keyword;

        public HostnameValidator(bool isIDNFormat = false)
        {
            var options = RegexOptions.IgnoreCase; // Hostnames are case-insensitive
            hostnameRegex = new Regex(asciiHostnamePattern, options, defaultMatchTimeout);
            performIDNConversion = isIDNFormat;
            keyword = isIDNFormat ? "format:idn-hostname" : "format:hostname";
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.String)
            {
                return ValidationResult.Ok;
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Ok;
            }

            if (performIDNConversion)
            {
                // Attempt to convert possible IDN to ASCII
                try
                {
                    instanceString = idn.GetAscii(instanceString);
                }
                catch (ArgumentException)
                {
                    // If conversion fails, the hostname is invalid
                    return new ValidationResult(keyword);
                }
            }

            if (IsValidHostname(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        private bool IsValidHostname(string hostname)
        {
            if (hostname.Length > 253 || hostname.Split('.').Any(label => label.Length > 63))
            {
                return false;
            }

            return hostnameRegex.IsMatch(hostname);
        }

    }
}