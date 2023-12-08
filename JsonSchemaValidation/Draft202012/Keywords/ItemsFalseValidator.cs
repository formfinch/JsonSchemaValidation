using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemsFalseValidator : IKeywordValidator
    {
        private readonly int _nPrefixItems;

        public ItemsFalseValidator(int nPrefixItems)
        {
            _nPrefixItems = nPrefixItems;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the Items keyword
                return ValidationResult.Ok;
            }

            if(_nPrefixItems < instance.GetArrayLength())
            {
                // push all array indices to evaluateditems

                return new ValidationResult("Invalid items");
            }

            return ValidationResult.Ok;
        }
    }
}