using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinLengthValidator : IKeywordValidator
    {
        private const string keyword = "minLength";
        private readonly int minLength;

        public MinLengthValidator(int minLength)
        {
            this.minLength = minLength;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.String)
            {
                // If the instance is not a string, it's considered valid with respect to the minLength keyword
                return ValidationResult.Ok;
            }

            var instanceString = instance.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Ok;  // This is a fallback; ideally, a JSON string should not be null.
            }

            StringInfo stringInfo = new(instanceString);
            int actualLength = stringInfo.LengthInTextElements;
            if (actualLength >= minLength)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }
    }
}