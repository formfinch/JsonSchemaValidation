using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemsTrueValidator : IKeywordValidator
    {
        public ItemsTrueValidator()
        {
            // No nPrefixItems because any additional items will then validate to true anyway
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the Items keyword
                return ValidationResult.Ok;
            }

            // push all array indices to evaluateditems

            // Any array will pass validation with respect to the items keyword.
            return ValidationResult.Ok;
        }
    }
}