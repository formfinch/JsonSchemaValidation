using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemsTrueValidator : IKeywordValidator
    {
        private readonly int _nPrefixItems;

        public string Keyword => "items";

        public ItemsTrueValidator(int nPrefixItems)
        {
            _nPrefixItems = nPrefixItems;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the items keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            if (_nPrefixItems < context.Data.GetArrayLength())
            {
                arrayContext.SetAllItemsEvaluated();
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
