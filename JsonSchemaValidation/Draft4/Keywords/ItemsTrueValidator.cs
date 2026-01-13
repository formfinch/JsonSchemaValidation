// Draft behavior: Identical in Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Boolean true schema for items - all items are valid.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft4.Keywords
{
    internal sealed class ItemsTrueValidator : IKeywordValidator
    {
        public string Keyword => "items";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data) => true;

        public bool IsValid(IJsonValidationContext context) => true;

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            // Mark all indices as evaluated
            if (context is IJsonValidationArrayContext arrayContext)
            {
                int idx = 0;
                foreach (var _ in context.Data.EnumerateArray())
                {
                    arrayContext.SetEvaluatedIndex(idx);
                    idx++;
                }
            }

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = true }
            };
        }
    }
}
