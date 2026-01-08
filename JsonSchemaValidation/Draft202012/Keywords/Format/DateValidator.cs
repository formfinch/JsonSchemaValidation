using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class DateValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex for date only ISO 8601 structure validation
        private static readonly Regex dateRegex = new Regex(
            @"^\d{4}-\d{2}-\d{2}$",
            RegexOptions.Compiled, defaultMatchTimeout);

        public string Keyword => "format";

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
            {
                // If the instance is not a string, it's considered valid with respect to the format keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (IsValidDate(instanceString))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation) with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = "date" }
                };
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid date");
        }

        private static bool IsValidDate(string date)
        {
            try
            {
                if (!dateRegex.IsMatch(date))
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
