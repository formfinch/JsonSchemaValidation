// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates RFC 3339 date format (YYYY-MM-DD).

using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords.Format
{
    internal sealed class DateValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex for date only ISO 8601 structure validation
        private static readonly Regex dateRegex = new Regex(
            @"^\d{4}-\d{2}-\d{2}$",
            RegexOptions.Compiled, defaultMatchTimeout);

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            return str == null || IsValidDate(str);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid date");

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = "date" }
            };
        }

        private static bool IsValidDate(string date)
        {
            try
            {
                if (!dateRegex.IsMatch(date))
                {
                    return false;
                }

                // Parse the date string to validate it
                return DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);
            }
            catch
            {
                return false;
            }
        }
    }
}
