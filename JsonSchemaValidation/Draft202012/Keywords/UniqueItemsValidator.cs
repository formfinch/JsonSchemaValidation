using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class UniqueItemsValidator : IKeywordValidator
    {
        public string Keyword => "uniqueItems";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Array)
                return true;

            int itemCount = data.GetArrayLength();
            for (int i = 0; i < itemCount; i++)
            {
                for (int j = i + 1; j < itemCount; j++)
                {
                    if (JsonElement.DeepEquals(data[i], data[j]))
                        return false;
                }
            }
            return true;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the uniqueItems keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            int itemCount = context.Data.GetArrayLength();

            for (int i = 0; i < itemCount; i++)
            {
                for (int j = i + 1; j < itemCount; j++)
                {
                    if (JsonElement.DeepEquals(context.Data[i], context.Data[j]))
                    {
                        return ValidationResult.Invalid(instanceLocation, kwLocation, $"Array items at indices {i} and {j} are not unique");
                    }
                }
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
