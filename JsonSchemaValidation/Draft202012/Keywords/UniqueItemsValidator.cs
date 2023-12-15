using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class UniqueItemsValidator : IKeywordValidator
    {
        private const string keyword = "uniqueItems";

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the minItems keyword
                return ValidationResult.Ok;
            }

            var comparer = new JsonElementComparison();
            int itemCount = context.Data.GetArrayLength();

            for (int i = 0; i < itemCount; i++)
            {
                for (int j = i + 1; j < itemCount; j++)
                {
                    if (comparer.DeepEquals(context.Data[i], context.Data[j]))
                    {
                        return new ValidationResult(keyword);
                    }
                }
            }

            return ValidationResult.Ok;
        }

    }
}