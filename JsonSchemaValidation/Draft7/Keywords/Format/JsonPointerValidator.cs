// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates JSON Pointer format per RFC 6901.

using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords.Format
{
    internal sealed class JsonPointerValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex pattern for JSON Pointer validation (compiled for performance)
        private static readonly Regex jsonPointerRegex = new Regex(
            @"^(\/([^/~]|(~[01]))*)*$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture, defaultMatchTimeout);

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            return str == null || IsValidJsonPointer(str);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid JSON pointer");

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = "json-pointer" }
            };
        }

        private static bool IsValidJsonPointer(string jsonPointer)
        {
            return jsonPointerRegex.IsMatch(jsonPointer);
        }
    }
}
