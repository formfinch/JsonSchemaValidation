using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class ItemValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly int _nPrefixItems;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "items";

        public ItemValidator(ISchemaValidator validator, int nPrefixItems, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _nPrefixItems = nPrefixItems;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the items keyword
                return true;
            }

            int idxItem = 0;
            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (idxItem >= _nPrefixItems)
                {
                    var itemContext = _contextFactory.CreateContextForArrayItemFast(context, item);
                    if (!_validator.IsValid(itemContext))
                    {
                        return false;
                    }
                }
                idxItem++;
            }

            return true;
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

            var children = new List<ValidationResult>();
            int idxItem = 0;
            bool validatedAnyItems = false;

            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (idxItem >= _nPrefixItems)
                {
                    validatedAnyItems = true;
                    var itemContext = _contextFactory.CreateContextForArrayItem(context, idxItem, item);
                    var itemValidationResult = _validator.Validate(itemContext, keywordLocation);
                    children.Add(itemValidationResult);

                    if (!itemValidationResult.IsValid)
                    {
                        return ValidationResult.Invalid(instanceLocation, kwLocation, $"Item at index {idxItem.ToString(System.Globalization.CultureInfo.InvariantCulture)} is invalid") with { Children = children };
                    }
                    arrayContext.SetEvaluatedIndex(idxItem);
                }
                idxItem++;
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
