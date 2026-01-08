using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class DurationValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Capturing Regex for ISO 8601 duration format validation with named groups
        private static readonly Regex durationRegex = new Regex(
            @"^P(?:(?<years>[0-9]+Y)?(?<weeks>[0-9]+W)?(?<months>[0-9]+M)?(?<days>[0-9]+D)?)"
            + @"(T(?<hours>[0-9]+H)?(?<minutes>[0-9]+M)?(?<seconds>[0-9]+S)?)?$",
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

            if (IsValidDuration(instanceString))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation) with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = "duration" }
                };
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid duration");
        }

        private static bool IsValidDuration(string duration)
        {
            var match = durationRegex.Match(duration);
            if (!match.Success)
            {
                return false; // Basic pattern not matched
            }

            // Validate components based on groups presence
            bool hasDateComponent = match.Groups["years"].Success || match.Groups["months"].Success || match.Groups["days"].Success;
            bool hasTimeComponent = match.Groups["hours"].Success || match.Groups["minutes"].Success || match.Groups["seconds"].Success;
            bool hasWeeksComponent = match.Groups["weeks"].Success;

            if (!hasDateComponent && !hasTimeComponent && !hasWeeksComponent)
            {
                // Must at least have one component
                return false;
            }

            if (duration.Contains('T') && !hasTimeComponent)
            {
                // Empty time components are not allowed
                return false;
            }

            if (hasWeeksComponent && (hasDateComponent || hasTimeComponent))
            {
                // Weeks cannot be combined with other components
                return false;
            }

            return true; // Passed all checks
        }
    }
}
