using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MaxLengthValidator : IKeywordValidator
    {
        private const string keyword = "maxLength";
        private readonly int maxLength;

        public MaxLengthValidator(int maxLength)
        {
            this.maxLength = maxLength;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.String)
            {
                // If the instance is not a string, it's considered valid with respect to the maxLength keyword
                return ValidationResult.Ok;
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Ok;  // This is a fallback; ideally, a JSON string should not be null.
            }

            StringInfo stringInfo = new(instanceString);
            int actualLength = stringInfo.LengthInTextElements;
            if (actualLength <= maxLength)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }
    }
}