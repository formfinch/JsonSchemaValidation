// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that string length is >= the minimum value (counting Unicode code points).

using System.Globalization;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft7.Keywords
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

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"String length is less than minimum length of {_minLength.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }
    }
}
