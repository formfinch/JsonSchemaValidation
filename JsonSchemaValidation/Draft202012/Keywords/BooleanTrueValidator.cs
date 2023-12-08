using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class BooleanTrueValidator : IKeywordValidator
    {
        public BooleanTrueValidator()
        {
        }

        public ValidationResult Validate(JsonElement instance)
        {
            return ValidationResult.Ok;
        }
    }
}