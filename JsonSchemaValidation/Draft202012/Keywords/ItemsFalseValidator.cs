using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class ItemsFalseValidator : IKeywordValidator
    {
        private readonly int _nPrefixItems;

        public string Keyword => "items";

        public ItemsFalseValidator(int nPrefixItems)
        {
            _nPrefixItems = nPrefixItems;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            // items: false means no items beyond prefixItems are allowed
            return _nPrefixItems >= context.Data.GetArrayLength();
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
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Array has more items ({context.Data.GetArrayLength().ToString(System.Globalization.CultureInfo.InvariantCulture)}) than allowed by prefixItems ({_nPrefixItems.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
