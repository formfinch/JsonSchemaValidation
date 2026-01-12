// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that string length is <= the maximum value (counting Unicode code points).

using System.Globalization;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft7.Keywords
{
    internal sealed class MaxLengthValidator : IKeywordValidator
    {
        private readonly int _maxLength;

        public string Keyword => "maxLength";

        public bool SupportsDirectValidation => true;

        public MaxLengthValidator(int maxLength)
        {
            _maxLength = maxLength;
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            if (str == null)
                return true;
            return new StringInfo(str).LengthInTextElements <= _maxLength;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"String length exceeds maximum length of {_maxLength.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }
    }
}
