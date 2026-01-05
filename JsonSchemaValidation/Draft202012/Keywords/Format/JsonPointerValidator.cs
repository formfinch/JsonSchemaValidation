using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class JsonPointerValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex pattern for JSON Pointer validation
        private static readonly string jsonPointerPattern = @"^(\/([^/~]|(~[01]))*)*$";

        private readonly Regex jsonPointerRegex;

        public string Keyword => "format";

        public JsonPointerValidator()
        {
            var options = RegexOptions.None;
            jsonPointerRegex = new Regex(jsonPointerPattern, options, defaultMatchTimeout);
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (IsValidJsonPointer(instanceString))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation) with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = "json-pointer" }
                };
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid JSON pointer");
        }

        private bool IsValidJsonPointer(string jsonPointer)
        {
            return jsonPointerRegex.IsMatch(jsonPointer);
        }
    }
}
