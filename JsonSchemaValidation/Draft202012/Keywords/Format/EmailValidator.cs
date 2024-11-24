using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class EmailValidator : IKeywordValidator
    {
        private const string keyword = "format:email";

        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex for email parts recognition
        private static readonly string basicStructure = @"^.+@.+$";
        private static readonly string localPart = @"^(?:(?:[\p{L}\p{N}!#$%&'*+\-/=?^_`{|}~]+(?:\.[\p{L}\p{N}!#$%&'*+\-/=?^_`{|}~]+)*)|(?:\"".+\""))$";

        private static readonly string quotedLocalPart = @"^""([\s\p{L}\p{N}!#$%&'*+\-\/=?^_`{|}~.,:;<>[\]\\\@]+)""$";
        private static readonly string domainPart = @"^(?:[\p{L}\p{N}-\.]+\.[\p{L}]{2,}|(?:\[(?:\d{1,3}\.){3}\d{1,3}\]|\[IPv6:[0-9a-fA-F:.]+\]))$";

        private readonly Regex basicStructureRegex;
        private readonly Regex localPartRegex;
        private readonly Regex quotedLocalPartRegex;
        private readonly Regex domainPartRegex;

        public EmailValidator()
        {
            var options = RegexOptions.None;
            basicStructureRegex = new Regex(basicStructure, options, defaultMatchTimeout);
            localPartRegex = new Regex(localPart, options, defaultMatchTimeout);
            quotedLocalPartRegex = new Regex(quotedLocalPart, options, defaultMatchTimeout);
            domainPartRegex = new Regex(domainPart, options, defaultMatchTimeout);
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
                return ValidationResult.Ok;  // This is a fallback; ideally, a JSON string should not be null.
            }

            if(IsValidEmail(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        public bool IsValidEmail(string email)
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

        private string[] SplitEmail(string email)
        {
            int atIndex = email.LastIndexOf('@');
            if (atIndex == -1) return Array.Empty<string>(); // No '@' symbol found

            string localPart = email.Substring(0, atIndex);
            string domainPart = email.Substring(atIndex + 1);

            return new string[] { localPart, domainPart };
        }

        private bool ValidateLocalPart(string localPart)
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

        private bool ValidateDomainPart(string domainPart)
        {
            if (domainPartRegex.IsMatch(domainPart))
            {
                // Additional check for valid IPv4 address literals
                if (domainPart.StartsWith("[") && domainPart.EndsWith("]"))
                {
                    var address = domainPart.Trim('[', ']');
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
