// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal sealed partial class DurationValidator : IKeywordValidator
    {
        // Capturing Regex for ISO 8601 duration format validation with named groups
        [GeneratedRegex(@"^P(?:(?<years>[0-9]+Y)?(?<weeks>[0-9]+W)?(?<months>[0-9]+M)?(?<days>[0-9]+D)?)(?:T(?<hours>[0-9]+H)?(?<minutes>[0-9]+M)?(?<seconds>[0-9]+S)?)?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 3000)]
        private static partial Regex DurationRegex();

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            return str == null || IsValidDuration(str);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid duration");

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = "duration" }
            };
        }

        private static bool IsValidDuration(string duration)
        {
            var match = DurationRegex().Match(duration);
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
