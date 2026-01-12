// Draft behavior: Identical in Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Boolean false schema for items - no items are valid (array must be empty).

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft7.Keywords
{
    internal sealed class ItemsFalseValidator : IKeywordValidator
    {
        public string Keyword => "items";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Array)
                return true;
            return data.GetArrayLength() == 0;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context.Data.GetArrayLength() > 0)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Items schema is false - array must be empty");
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
