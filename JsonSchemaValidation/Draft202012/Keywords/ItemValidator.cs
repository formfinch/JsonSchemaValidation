using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly int _nPrefixItems;
        private readonly IJsonValidationContextFactory _contextFactory;

        public ItemValidator(ISchemaValidator validator, int nPrefixItems, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _nPrefixItems = nPrefixItems;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the items keyword
                return ValidationResult.Ok;
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            int idxItem = 0;
            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (idxItem++ >= _nPrefixItems)
                {
                    var itemContext = _contextFactory.CreateContextForArrayItem(context, idxItem - 1, item);
                    var itemValidationResult = _validator.Validate(itemContext);
                    if (itemValidationResult != ValidationResult.Ok)
                    {
                        return itemValidationResult;
                    }
                    arrayContext.SetEvaluatedIndex(idxItem - 1);
                }
            }
            return ValidationResult.Ok;
        }
    }
}