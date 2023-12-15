using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class UnevaluatedItemsValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _unevaluatedItemValidator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public UnevaluatedItemsValidator(ISchemaValidator unevaluatedItemValidator, IJsonValidationContextFactory contextFactory)
        {
            _unevaluatedItemValidator = unevaluatedItemValidator;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the UnevaluatedItems keyword
                return ValidationResult.Ok;
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            foreach (JsonElement item in arrayContext.GetUnevaluatedItems())
            {
                var itemContext = _contextFactory.CreateContextForRoot(item);
                var itemValidationResult = _unevaluatedItemValidator.Validate(itemContext);
                if (itemValidationResult != ValidationResult.Ok)
                {
                    return itemValidationResult;
                }
            }
            arrayContext.SetUnevaluatedItemsEvaluated();
            return ValidationResult.Ok;
        }
    }
}