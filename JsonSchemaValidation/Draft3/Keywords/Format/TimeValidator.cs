// Draft 3 behavior: Validates time format (HH:MM:SS).
// Note: Draft 3 time format does NOT require timezone suffix.

using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft3.Keywords.Format
{
    internal sealed class TimeValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex for Draft 3 time format (HH:MM:SS, optional timezone)
        // Draft 3 spec does not require timezone, just HH:MM:SS format
        private static readonly Regex timeRegex = new Regex(
            @"^(?<hour>[01][0-9]|2[0-3]):(?<minute>[0-5][0-9]):(?<second>[0-5][0-9])$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture, defaultMatchTimeout);

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var time = data.GetString();
            if (time == null)
                return false;
            return IsValidTime(time);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        private static bool IsValidTime(string time)
        {
            // Draft 3 time format: HH:MM:SS (no timezone required)
            return timeRegex.IsMatch(time);
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid time");

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = "time" }
            };
        }
    }
}
