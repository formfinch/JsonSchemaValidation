using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeStringValidator : IKeywordValidator
    {
        public TypeStringValidator()
        {
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind == JsonValueKind.String)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult($"Expected type string");
        }
    }
}
