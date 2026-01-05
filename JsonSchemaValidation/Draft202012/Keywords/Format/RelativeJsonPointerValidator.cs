using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class RelativeJsonPointerValidator : IKeywordValidator
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        // Regex pattern for Relative JSON Pointer validation
        private static readonly string relativeJsonPointerPattern = @"^((0#?)|([1-9]\d*#?))(\/([^/~]|(~[01]))*)*$";

        private readonly Regex relativeJsonPointerRegex;

        public string Keyword => "format";

        public RelativeJsonPointerValidator()
        {
            var options = RegexOptions.None;
            relativeJsonPointerRegex = new Regex(relativeJsonPointerPattern, options, defaultMatchTimeout);
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

            if (IsValidRelativeJsonPointer(instanceString))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation) with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = "relative-json-pointer" }
                };
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value is not a valid relative JSON pointer");
        }

        private bool IsValidRelativeJsonPointer(string relativeJsonPointer)
        {
            if (string.IsNullOrWhiteSpace(relativeJsonPointer))
            {
                return false;
            }

            return relativeJsonPointerRegex.IsMatch(relativeJsonPointer);
        }
    }
}
