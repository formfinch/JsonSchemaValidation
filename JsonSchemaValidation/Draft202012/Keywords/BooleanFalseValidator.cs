using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class BooleanFalseValidator : IKeywordValidator
    {
        public string Keyword => "";  // Boolean schema has no keyword name

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data) => false;

        public bool IsValid(IJsonValidationContext context) => false;

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            return ValidationResult.Invalid(context.InstanceLocation.ToString(), keywordLocation.ToString(), "Schema is false - all values are invalid");
        }
    }
}
