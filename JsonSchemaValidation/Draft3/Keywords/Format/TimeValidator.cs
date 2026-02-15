// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft 3 behavior: Validates time format (HH:MM:SS).
// Note: Draft 3 time format does NOT require timezone suffix.

using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords.Format
{
    internal sealed partial class TimeValidator : IKeywordValidator
    {
        // Regex for Draft 3 time format (HH:MM:SS, optional timezone)
        // Draft 3 spec does not require timezone, just HH:MM:SS format
        [GeneratedRegex(@"^(?<hour>[01][0-9]|2[0-3]):(?<minute>[0-5][0-9]):(?<second>[0-5][0-9])$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 3000)]
        private static partial Regex TimeRegex();

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
            return TimeRegex().IsMatch(time);
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
