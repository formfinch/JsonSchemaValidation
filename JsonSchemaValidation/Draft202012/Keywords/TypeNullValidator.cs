using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeNullValidator : IKeywordValidator
    {
        public TypeNullValidator()
        {
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind == JsonValueKind.Null)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult($"Expected null");
        }
    }
}