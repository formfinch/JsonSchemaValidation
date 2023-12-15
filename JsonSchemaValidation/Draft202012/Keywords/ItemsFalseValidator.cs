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

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the Items keyword
                return ValidationResult.Ok;
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            if (_nPrefixItems < context.Data.GetArrayLength())
            {
                return new ValidationResult("Invalid items");
            }

            return ValidationResult.Ok;
        }
    }
}