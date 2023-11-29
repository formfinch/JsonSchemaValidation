using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeIntegerValidator : IKeywordValidator
    {
        private ValidationResult validationFailed = new("Expected an integer value");

        public TypeIntegerValidator()
        {
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Number)
            {
                return validationFailed;
            }

            if(!instance.TryGetDecimal(out decimal value))
            {
                return validationFailed;
            }

            if(value != decimal.Truncate(value))
            {
                return validationFailed;
            }

            return ValidationResult.Ok;
        }
    }
}

