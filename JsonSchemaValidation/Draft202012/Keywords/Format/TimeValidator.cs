using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class TimeValidator : IKeywordValidator
    {
        private const string keyword = "format:time";
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex for time ISO 8601 structure validation
        private static readonly string iso8601TimePattern = @"^([01]\d|2[0-3]):([0-5]\d)(:([0-5]\d)(\.\d+)?)?([zZ]|[+-]\d{2}:\d{2})$";  // Basic pattern for HH:mm:ss and optional fractional seconds

        private readonly Regex timeRegex;

        public TimeValidator()
        {
            var options = RegexOptions.None;
            timeRegex = new Regex(iso8601TimePattern, options, defaultMatchTimeout);
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
                return ValidationResult.Ok; // This is a fallback; ideally, a JSON string should not be null.
            }

            if (IsValidTime(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        private bool IsValidTime(string time)
        {
            try
            {
                if(!timeRegex.IsMatch(time))
                {
                    return false;
                }

                if (!DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
