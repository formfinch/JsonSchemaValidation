using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PrefixItemFalseValidator : IKeywordValidator
    {
        private readonly int _prefixItemIndex;

        public PrefixItemFalseValidator(int prefixItemIndex)
        {
            _prefixItemIndex = prefixItemIndex;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the PrefixItems keyword
                return ValidationResult.Ok;
            }

            if(_prefixItemIndex < instance.GetArrayLength())
            {
                return new ValidationResult("Invalid prefixItems");
            }

            return ValidationResult.Ok;
        }
    }
}