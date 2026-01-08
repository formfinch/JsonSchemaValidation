using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class EnumValidator : IKeywordValidator
    {
        private readonly JsonElement _enumValuesElement;

        public string Keyword => "enum";

        public EnumValidator(JsonElement enumValuesElement)
        {
            _enumValuesElement = enumValuesElement;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (_enumValuesElement.EnumerateArray().Any(value => JsonElement.DeepEquals(value, context.Data)))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value must be one of the enumerated values");
        }
    }
}
