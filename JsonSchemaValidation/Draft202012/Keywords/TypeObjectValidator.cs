using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeObjectValidator : IKeywordValidator
    {
        private ValidationResult validationFailed = new("Expected an object");

        public TypeObjectValidator()
        {
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Object)
            {
                return validationFailed;
            }

            return ValidationResult.Ok;
        }
    }
}

