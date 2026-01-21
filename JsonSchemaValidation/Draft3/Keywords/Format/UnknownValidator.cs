// Draft behavior: Identical in Draft 3, Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Unknown format validator - validates non-empty strings.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords.Format
{
    internal sealed class UnknownValidator : IKeywordValidator
    {
        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            return str == null || IsValidUnknown(str);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

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

            if (IsValidUnknown(instanceString))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not valid");
        }

        private static bool IsValidUnknown(string identifier)
        {
            return !string.IsNullOrWhiteSpace(identifier);
        }
    }
}
