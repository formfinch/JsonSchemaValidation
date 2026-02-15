// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates RFC 3339 date-time format.

using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords.Format
{
    internal sealed partial class DateTimeValidator : IKeywordValidator
    {
        // RFC 3339 date-time: YYYY-MM-DDThh:mm:ss[.frac](Z|±hh:mm) - ASCII digits only
        [GeneratedRegex(@"^(?<year>[0-9]{4})-(?<month>[0-9]{2})-(?<day>[0-9]{2})[tT](?<hour>[0-9]{2}):(?<minute>[0-9]{2}):(?<second>[0-5][0-9]|60)(?:\.[0-9]+)?(?:[zZ]|(?<sign>[+-])(?<offsetHour>[0-9]{2}):(?<offsetMinute>[0-9]{2}))$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 3000)]
        private static partial Regex DateTimeRegex();

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var dt = data.GetString();
            if (dt == null)
                return false;
            return IsValidDateTime(dt);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        private static bool IsValidDateTime(string dt)
        {
            var match = DateTimeRegex().Match(dt);
            if (!match.Success)
                return false;

            int year = int.Parse(match.Groups["year"].ValueSpan);
            int month = int.Parse(match.Groups["month"].ValueSpan);
            int day = int.Parse(match.Groups["day"].ValueSpan);
            int hour = int.Parse(match.Groups["hour"].ValueSpan);
            int minute = int.Parse(match.Groups["minute"].ValueSpan);
            int second = int.Parse(match.Groups["second"].ValueSpan);

            // Validate date
            if (month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
                return false;

            // Validate offset range if numeric
            if (match.Groups["sign"].Success)
            {
                int offsetHours = int.Parse(match.Groups["offsetHour"].ValueSpan);
                int offsetMinutes = int.Parse(match.Groups["offsetMinute"].ValueSpan);
                if (offsetHours > 23 || offsetMinutes > 59)
                    return false;
            }

            // Leap second validation (only when seconds == 60)
            if (second == 60 && !IsValidLeapSecond(hour, minute, match))
                return false;

            return true;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid date-time");

            // Per spec: format always produces an annotation with the format name
            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = "date-time" }
            };
        }

        private static bool IsValidLeapSecond(int hour, int minute, Match match)
        {
            // Leap second only valid at 23:59:60 UTC
            if (!match.Groups["sign"].Success) // Zulu time
                return hour == 23 && minute == 59;

            int offsetHours = int.Parse(match.Groups["offsetHour"].ValueSpan);
            int offsetMinutes = int.Parse(match.Groups["offsetMinute"].ValueSpan);

            int localMinutes = hour * 60 + minute;
            int offsetTotalMinutes = offsetHours * 60 + offsetMinutes;
            int utcMinutes = string.Equals(match.Groups["sign"].Value, "+", StringComparison.Ordinal)
                ? localMinutes - offsetTotalMinutes
                : localMinutes + offsetTotalMinutes;

            return ((utcMinutes % 1440) + 1440) % 1440 == 23 * 60 + 59;
        }
    }
}
