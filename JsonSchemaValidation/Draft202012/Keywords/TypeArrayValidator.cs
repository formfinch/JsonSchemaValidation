using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeArrayValidator : IKeywordValidator
    {
        private ValidationResult validationFailed = new("Expected an array");

        public TypeArrayValidator()
        {
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                return validationFailed;
            }

            return ValidationResult.Ok;
        }
    }
}

