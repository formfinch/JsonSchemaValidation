using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemsFalseValidator : IKeywordValidator
    {
        private readonly int _nPrefixItems;

        public string Keyword => "items";

        public ItemsFalseValidator(int nPrefixItems)
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

            if (context is not IJsonValidationArrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            if (_nPrefixItems < context.Data.GetArrayLength())
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Array has more items ({context.Data.GetArrayLength()}) than allowed by prefixItems ({_nPrefixItems})");
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
