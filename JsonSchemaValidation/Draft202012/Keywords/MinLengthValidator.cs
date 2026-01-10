using System.Globalization;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class MinLengthValidator : IKeywordValidator
    {
        private readonly int _minLength;

        public string Keyword => "minLength";

        public bool SupportsDirectValidation => true;

        public MinLengthValidator(int minLength)
        {
            _minLength = minLength;
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            if (str == null)
                return true;
            return new StringInfo(str).LengthInTextElements >= _minLength;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
            {
                // If the instance is not a string, it's considered valid with respect to the minLength keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            StringInfo stringInfo = new(instanceString);
            int actualLength = stringInfo.LengthInTextElements;
            if (actualLength >= _minLength)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"String length {actualLength} is less than minimum length of {_minLength}");
        }
    }
}
