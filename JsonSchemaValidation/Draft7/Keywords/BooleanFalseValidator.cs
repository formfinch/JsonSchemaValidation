// Draft behavior: Identical in Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Boolean false schema - all values are invalid.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft7.Keywords
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
