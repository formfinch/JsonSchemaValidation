using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PrefixItemsValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public PrefixItemsValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the PrefixItems keyword
                return ValidationResult.Ok;
            }

            if (!_validators.Any())
            {
                return ValidationResult.Ok;
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            int prefixItemIndex = 0;
            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (prefixItemIndex >= _validators.Count())
                    break;

                var validator = _validators.ElementAt(prefixItemIndex);
                var itemContext = _contextFactory.CreateContextForArrayItem(context, prefixItemIndex++, item);
                var itemValidationResult = validator.Validate(itemContext);
                if (itemValidationResult != ValidationResult.Ok)
                {
                    return itemValidationResult;
                }
                arrayContext.SetEvaluatedIndex(prefixItemIndex - 1);
            }

            return ValidationResult.Ok;
        }
    }
}