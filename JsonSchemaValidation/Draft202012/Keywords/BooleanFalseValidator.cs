using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class BooleanFalseValidator : IKeywordValidator
    {
        public string Keyword => "";  // Boolean schema has no keyword name

        public BooleanFalseValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            return ValidationResult.Invalid(context.InstanceLocation.ToString(), keywordLocation.ToString(), "Schema is false - all values are invalid");
        }
    }
}