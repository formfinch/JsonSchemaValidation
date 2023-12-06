using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly int _nPrefixItems;

        public ItemValidator(ISchemaValidator validator, int nPrefixItems)
        {
            _validator = validator;
            _nPrefixItems = nPrefixItems;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the items keyword
                return ValidationResult.Ok;
            }

            int idxItem = 0;
            foreach (JsonElement item in instance.EnumerateArray())
            {
                if (idxItem++ >= _nPrefixItems)
                {
                    var itemValidationResult = _validator.Validate(item);
                    if (itemValidationResult != ValidationResult.Ok)
                    {
                        return itemValidationResult;
                    }
                }
            }
            return ValidationResult.Ok;
        }
    }
}