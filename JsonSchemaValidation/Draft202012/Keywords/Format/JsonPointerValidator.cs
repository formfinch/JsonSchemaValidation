using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class JsonPointerValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);
        private const string keyword = "format:json-pointer";

        // Regex pattern for JSON Pointer validation
        private static readonly string jsonPointerPattern = @"^(\/([^/~]|(~[01]))*)*$";

        private readonly Regex jsonPointerRegex;

        public JsonPointerValidator()
        {
            var options = RegexOptions.None;
            jsonPointerRegex = new Regex(jsonPointerPattern, options, defaultMatchTimeout);
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.String)
            {
                return ValidationResult.Ok;
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Ok;
            }

            if (IsValidJsonPointer(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        private bool IsValidJsonPointer(string jsonPointer)
        {
            return jsonPointerRegex.IsMatch(jsonPointer);
        }
    }
}
