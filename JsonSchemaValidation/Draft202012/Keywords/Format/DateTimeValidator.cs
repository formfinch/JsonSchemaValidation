// todo: DateTime should split on [tT] separator into Date and Time components. 
// These two components should be validated by Date and Time validation that is shared with DateValidator and TimeValidator.
// Goal is to keep regex for DateTime and separate components consistent and defined in one single spot.
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class DateTimeValidator : IKeywordValidator
    {
        private const string keyword = "format:datetime";
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex for basic ISO 8601 structure validation
        private static readonly string iso8601BasicPattern = @"^\d{4}-\d{2}-\d{2}[tT]\d{2}:\d{2}:\d{2}(\.\d+)?([zZ]|[+-]\d{2}:\d{2})?$";

        private readonly Regex dateTimeRegex;

        public DateTimeValidator()
        {
            var options = RegexOptions.None;
            dateTimeRegex = new Regex(iso8601BasicPattern, options, defaultMatchTimeout);
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

            if (IsValidDateTime(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        private bool IsValidDateTime(string dateTime)
        {
            try
            {
                if(!dateTimeRegex.IsMatch(dateTime))
                {
                    return false;
                }

                if (!DateTimeOffset.TryParse(dateTime, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal, out _))
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
