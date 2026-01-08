using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class EmailValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex for email parts recognition (compiled for performance)
        private static readonly Regex basicStructureRegex = new Regex(
            @"^.+@.+$",
            RegexOptions.Compiled, defaultMatchTimeout);
        private static readonly Regex localPartRegex = new Regex(
            @"^(?:(?:[\p{L}\p{N}!#$%&'*+\-/=?^_`{|}~]+(?:\.[\p{L}\p{N}!#$%&'*+\-/=?^_`{|}~]+)*)|(?:\"".+\""))$",
            RegexOptions.Compiled, defaultMatchTimeout);
        private static readonly Regex quotedLocalPartRegex = new Regex(
            @"^""([\s\p{L}\p{N}!#$%&'*+\-\/=?^_`{|}~.,:;<>[\]\\\@]+)""$",
            RegexOptions.Compiled, defaultMatchTimeout);
        private static readonly Regex domainPartRegex = new Regex(
            @"^(?:[\p{L}\p{N}-\.]+\.[\p{L}]{2,}|(?:\[(?:\d{1,3}\.){3}\d{1,3}\]|\[IPv6:[0-9a-fA-F:.]+\]))$",
            RegexOptions.Compiled, defaultMatchTimeout);

        public string Keyword => "format";

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

            if (IsValidEmail(instanceString))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation) with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = "email" }
                };
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid email address");
        }

        public static bool IsValidEmail(string email)
        {
            // Length check
            if (email.Length > 254) return false;

            // Basic structure check
            if (!basicStructureRegex.IsMatch(email)) return false;

            // Custom split to handle quoted local parts
            var parts = SplitEmail(email);
            if (parts == null || parts.Length != 2) return false; // Ensure correct splitting

            var localPart = parts[0];
            var domainPart = parts[1];

            // Validate local and domain parts
            if (!ValidateLocalPart(localPart)) return false;
            if (!ValidateDomainPart(domainPart)) return false;

            return true;
        }

        private static string[] SplitEmail(string email)
        {
            int atIndex = email.LastIndexOf('@');
            if (atIndex == -1) return Array.Empty<string>(); // No '@' symbol found

            string localPart = email.Substring(0, atIndex);
            string domainPart = email.Substring(atIndex + 1);

            return new string[] { localPart, domainPart };
        }

        private static bool ValidateLocalPart(string localPart)
        {
            if (localPart.StartsWith("\"") && localPart.EndsWith("\""))
            {
                return quotedLocalPartRegex.IsMatch(localPart);
            }
            else
            {
                return localPartRegex.IsMatch(localPart);
            }
        }

        private static bool ValidateDomainPart(string domain)
        {
            if (domainPartRegex.IsMatch(domain))
            {
                // Additional check for valid IPv4 address literals
                if (domain.StartsWith("[") && domain.EndsWith("]"))
                {
                    var address = domain.Trim('[', ']');
                    if (address.StartsWith("IPv6:"))
                    {
                        // IPv6 literal; no further validation in this context
                        return true;
                    }
                    else
                    {
                        // Validate IPv4 address
                        return IPAddress.TryParse(address, out _);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
