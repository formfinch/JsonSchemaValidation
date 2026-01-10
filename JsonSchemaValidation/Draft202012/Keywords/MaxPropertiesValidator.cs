using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class MaxPropertiesValidator : IKeywordValidator
    {
        private readonly int _maxProperties;

        public string Keyword => "maxProperties";

        public bool SupportsDirectValidation => true;

        public MaxPropertiesValidator(int maxProperties)
        {
            _maxProperties = maxProperties;
        }

        public bool IsValid(JsonElement data) =>
            data.ValueKind != JsonValueKind.Object || data.EnumerateObject().Count() <= _maxProperties;

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Object exceeds the maximum of {_maxProperties.ToString(System.Globalization.CultureInfo.InvariantCulture)} properties");
        }
    }
}
