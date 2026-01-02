using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class TimeValidator : IKeywordValidator
    {
        private const string keyword = "format:time";
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex for time RFC 3339 structure validation (ASCII digits only)
        private static readonly Regex timeRegex = new Regex(
            @"^([01][0-9]|2[0-3]):([0-5][0-9]):([0-5][0-9]|60)(\.[0-9]+)?([zZ]|([+-])([0-9]{2}):([0-9]{2}))$",
            RegexOptions.None, defaultMatchTimeout);

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Ok;

            var time = context.Data.GetString();
            if (time == null)
                return new ValidationResult(keyword);

            var match = timeRegex.Match(time);
            if (!match.Success)
                return new ValidationResult(keyword);

            int hour = int.Parse(match.Groups[1].ValueSpan);
            int minute = int.Parse(match.Groups[2].ValueSpan);
            int second = int.Parse(match.Groups[3].ValueSpan);

            // Leap second validation (only if seconds == 60)
            if (second == 60 && !IsValidLeapSecond(hour, minute, match))
                return new ValidationResult(keyword);

            // For non-leap-seconds, use DateTimeOffset.TryParse for additional validation
            if (second < 60 && !DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return new ValidationResult(keyword);

            // Validate offset range if numeric offset present
            if (match.Groups[6].Success)
            {
                int offsetHours = int.Parse(match.Groups[7].ValueSpan);
                int offsetMinutes = int.Parse(match.Groups[8].ValueSpan);
                if (offsetHours > 23 || offsetMinutes > 59)
                    return new ValidationResult(keyword);
            }

            return ValidationResult.Ok;
        }

        private static bool IsValidLeapSecond(int hour, int minute, Match match)
        {
            // Leap second only valid at 23:59:60 UTC
            if (!match.Groups[6].Success) // Zulu time
                return hour == 23 && minute == 59;

            // Calculate UTC time: UTC = local - offset
            int offsetHours = int.Parse(match.Groups[7].ValueSpan);
            int offsetMinutes = int.Parse(match.Groups[8].ValueSpan);
            if (offsetHours > 23 || offsetMinutes > 59)
                return false;

            int localMinutes = hour * 60 + minute;
            int offsetTotalMinutes = offsetHours * 60 + offsetMinutes;
            int utcMinutes = match.Groups[6].Value == "+"
                ? localMinutes - offsetTotalMinutes
                : localMinutes + offsetTotalMinutes;

            return ((utcMinutes % 1440) + 1440) % 1440 == 23 * 60 + 59;
        }
    }
}
