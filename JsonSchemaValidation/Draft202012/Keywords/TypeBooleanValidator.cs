using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeBooleanValidator : IKeywordValidator
    {
        private ValidationResult validationFailed = new("Expected a boolean value");

        public TypeBooleanValidator()
        {
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.False
                && instance.ValueKind != JsonValueKind.True)
            {
                return validationFailed;
            }

            return ValidationResult.Ok;
        }
    }
}

