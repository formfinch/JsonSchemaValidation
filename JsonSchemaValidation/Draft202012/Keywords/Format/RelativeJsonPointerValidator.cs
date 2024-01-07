using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class RelativeJsonPointerValidator : IKeywordValidator
    {
        private const string keyword = "format:relative-json-pointer";
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex pattern for Relative JSON Pointer validation
        private static readonly string relativeJsonPointerPattern = @"^((0#?)|([1-9]\d*#?))(\/([^/~]|(~[01]))*)*$";

        private readonly Regex relativeJsonPointerRegex;

        public RelativeJsonPointerValidator()
        {
            var options = RegexOptions.None;
            relativeJsonPointerRegex = new Regex(relativeJsonPointerPattern, options, defaultMatchTimeout);
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

            if (IsValidRelativeJsonPointer(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        private bool IsValidRelativeJsonPointer(string relativeJsonPointer)
        {
            if(string.IsNullOrWhiteSpace(relativeJsonPointer))
            {
                return false;
            }

            return relativeJsonPointerRegex.IsMatch(relativeJsonPointer);
        }
    }
}
