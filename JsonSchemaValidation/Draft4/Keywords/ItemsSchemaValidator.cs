// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09
// Note: In Draft 2020-12, "items" only applies after "prefixItems", not as a single schema for all items.
// Validates all array items against a single schema when "items" is a schema (not an array).

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft4.Keywords
{
    internal sealed class ItemsSchemaValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "items";

        public ItemsSchemaValidator(ISchemaValidator validator, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                var itemContext = _contextFactory.CreateContextForArrayItemFast(context, item);
                if (!_validator.IsValid(itemContext))
                {
                    return false;
                }
            }

            return true;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            var children = new List<ValidationResult>();
            int idx = 0;
            bool validatedAnyItems = false;

            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                validatedAnyItems = true;
                var itemContext = _contextFactory.CreateContextForArrayItem(context, idx, item);
                var itemValidationResult = _validator.Validate(itemContext, keywordLocation);
                children.Add(itemValidationResult);

                if (!itemValidationResult.IsValid)
                {
                    return ValidationResult.Invalid(instanceLocation, kwLocation, $"Item at index {idx.ToString(System.Globalization.CultureInfo.InvariantCulture)} is invalid") with { Children = children };
                }

                arrayContext.SetEvaluatedIndex(idx);
                idx++;
            }

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with true if items keyword validated any items
            if (validatedAnyItems)
            {
                return result with
                {
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = true }
                };
            }

            return result;
        }
    }
}
