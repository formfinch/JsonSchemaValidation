// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal sealed partial class TimeValidator : IKeywordValidator
    {
        // Regex for time RFC 3339 structure validation (ASCII digits only)
        [GeneratedRegex(@"^(?<hour>[01][0-9]|2[0-3]):(?<minute>[0-5][0-9]):(?<second>[0-5][0-9]|60)(?:\.[0-9]+)?(?:[zZ]|(?<sign>[+-])(?<offsetHour>[0-9]{2}):(?<offsetMinute>[0-9]{2}))$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 3000)]
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
            var match = TimeRegex().Match(time);
            if (!match.Success)
                return false;

            int hour = int.Parse(match.Groups["hour"].ValueSpan);
            int minute = int.Parse(match.Groups["minute"].ValueSpan);
            int second = int.Parse(match.Groups["second"].ValueSpan);

            // Leap second validation
            if (second == 60 && !IsValidLeapSecond(hour, minute, match))
                return false;

            // For non-leap-seconds, use DateTimeOffset.TryParse for additional validation
            if (second < 60 && !DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return false;

            // Validate offset range if numeric offset present
            if (match.Groups["sign"].Success)
            {
                int offsetHours = int.Parse(match.Groups["offsetHour"].ValueSpan);
                int offsetMinutes = int.Parse(match.Groups["offsetMinute"].ValueSpan);
                if (offsetHours > 23 || offsetMinutes > 59)
                    return false;
            }

            return true;
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

        private static bool IsValidLeapSecond(int hour, int minute, Match match)
        {
            // Leap second only valid at 23:59:60 UTC
            if (!match.Groups["sign"].Success) // Zulu time
                return hour == 23 && minute == 59;

            // Calculate UTC time: UTC = local - offset
            int offsetHours = int.Parse(match.Groups["offsetHour"].ValueSpan);
            int offsetMinutes = int.Parse(match.Groups["offsetMinute"].ValueSpan);
            if (offsetHours > 23 || offsetMinutes > 59)
                return false;

            int localMinutes = hour * 60 + minute;
            int offsetTotalMinutes = offsetHours * 60 + offsetMinutes;
            int utcMinutes = string.Equals(match.Groups["sign"].Value, "+", StringComparison.Ordinal)
                ? localMinutes - offsetTotalMinutes
                : localMinutes + offsetTotalMinutes;

            return ((utcMinutes % 1440) + 1440) % 1440 == 23 * 60 + 59;
        }
    }
}
