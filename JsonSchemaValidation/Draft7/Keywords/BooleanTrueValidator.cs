// Draft behavior: Identical in Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Boolean true schema - all values are valid.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft7.Keywords
{
    internal sealed class BooleanTrueValidator : IKeywordValidator
    {
        public string Keyword => "";  // Boolean schema has no keyword name

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data) => true;

        public bool IsValid(IJsonValidationContext context) => true;

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            return ValidationResult.Valid(context.InstanceLocation.ToString(), keywordLocation.ToString());
        }
    }
}
