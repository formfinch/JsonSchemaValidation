using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeNumberValidator : IKeywordValidator
    {
        public TypeNumberValidator()
        {
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind == JsonValueKind.Number)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult($"Expected a number");
        }
    }
}
