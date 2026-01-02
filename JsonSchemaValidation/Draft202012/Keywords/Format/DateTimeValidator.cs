using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class DateTimeValidator : IKeywordValidator
    {
        private const string keyword = "format:datetime";
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // RFC 3339 date-time: YYYY-MM-DDThh:mm:ss[.frac](Z|±hh:mm) - ASCII digits only
        private static readonly Regex dateTimeRegex = new Regex(
            @"^([0-9]{4})-([0-9]{2})-([0-9]{2})[tT]([0-9]{2}):([0-9]{2}):([0-5][0-9]|60)(\.[0-9]+)?([zZ]|([+-])([0-9]{2}):([0-9]{2}))$",
            RegexOptions.None, defaultMatchTimeout);

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Ok;

            var dt = context.Data.GetString();
            if (dt == null)
                return new ValidationResult(keyword);

            var match = dateTimeRegex.Match(dt);
            if (!match.Success)
                return new ValidationResult(keyword);

            int year = int.Parse(match.Groups[1].ValueSpan);
            int month = int.Parse(match.Groups[2].ValueSpan);
            int day = int.Parse(match.Groups[3].ValueSpan);
            int hour = int.Parse(match.Groups[4].ValueSpan);
            int minute = int.Parse(match.Groups[5].ValueSpan);
            int second = int.Parse(match.Groups[6].ValueSpan);

            // Validate date
            if (month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
                return new ValidationResult(keyword);

            // Validate offset range if numeric
            if (match.Groups[9].Success)
            {
                int offsetHours = int.Parse(match.Groups[10].ValueSpan);
                int offsetMinutes = int.Parse(match.Groups[11].ValueSpan);
                if (offsetHours > 23 || offsetMinutes > 59)
                    return new ValidationResult(keyword);
            }

            // Leap second validation (only when seconds == 60)
            if (second == 60 && !IsValidLeapSecond(hour, minute, match))
                return new ValidationResult(keyword);

            return ValidationResult.Ok;
        }

        private static bool IsValidLeapSecond(int hour, int minute, Match match)
        {
            // Leap second only valid at 23:59:60 UTC
            if (!match.Groups[9].Success) // Zulu time
                return hour == 23 && minute == 59;

            int offsetHours = int.Parse(match.Groups[10].ValueSpan);
            int offsetMinutes = int.Parse(match.Groups[11].ValueSpan);

            int localMinutes = hour * 60 + minute;
            int offsetTotalMinutes = offsetHours * 60 + offsetMinutes;
            int utcMinutes = match.Groups[9].Value == "+"
                ? localMinutes - offsetTotalMinutes
                : localMinutes + offsetTotalMinutes;

            return ((utcMinutes % 1440) + 1440) % 1440 == 23 * 60 + 59;
        }
    }
}
