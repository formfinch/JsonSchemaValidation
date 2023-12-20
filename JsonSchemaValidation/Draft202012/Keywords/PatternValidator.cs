using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PatternValidator : IKeywordValidator
    {
        private const string keyword = "pattern";
        private readonly Regex _rxPattern;

        public PatternValidator(Regex patternMatcher)
        {
            _rxPattern = patternMatcher;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.String)
            {
                // If the instance is not a string, it's considered valid with respect to the pattern keyword
                return ValidationResult.Ok;
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Ok;  // This is a fallback; ideally, a JSON string should not be null.
            }

            if(_rxPattern.IsMatch(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }
    }
}