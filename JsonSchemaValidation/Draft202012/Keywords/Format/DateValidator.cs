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
    internal class DateValidator : IKeywordValidator
    {
        private const string keyword = "format:date";
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex for date only ISO 8601 structure validation
        private static readonly string iso8601DatePattern = @"^\d{4}-\d{2}-\d{2}$";

        private readonly Regex dateRegex;

        public DateValidator()
        {
            var options = RegexOptions.None;
            dateRegex = new Regex(iso8601DatePattern, options, defaultMatchTimeout);
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

            if (IsValidDate(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        private bool IsValidDate(string date)
        {
            try
            {
                if(!dateRegex.IsMatch(date))
                {
                    return false;
                }

                // Parse the date string to a DateTime object
                if (!DateTime.TryParse(date, out DateTime parsedDate))
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
