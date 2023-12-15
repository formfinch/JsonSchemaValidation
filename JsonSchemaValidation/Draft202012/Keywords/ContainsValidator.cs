using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ContainsValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public ContainsValidator(ISchemaValidator validator, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the contains keyword
                return ValidationResult.Ok;
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            var containsResult = new ValidationResult("No array item matches with the 'contains' schema.");
            int idx = 0;
            var containsIndices = new List<int>();
            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                var itemContext = _contextFactory.CreateContextForArrayItem(context, idx++, item);
                var itemValidationResult = _validator.Validate(itemContext);
                if (itemValidationResult == ValidationResult.Ok)
                {
                    containsIndices.Add(idx - 1);
                    containsResult = ValidationResult.Ok;
                }
            }

            if(containsResult == ValidationResult.Ok)
            {
                arrayContext.SetEvaluatedIndices(containsIndices);
            }

            return containsResult;
        }
    }
}