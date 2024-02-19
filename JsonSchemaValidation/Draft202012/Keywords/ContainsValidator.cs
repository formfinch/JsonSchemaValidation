using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;
using System.Xml.Linq;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ContainsValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;
        
        public int? MinContains { get; set; }
        public int? MaxContains { get; set; }

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

            if (MinContains.HasValue && MinContains.Value == 0 
                && containsIndices.Count == 0)
            {
                return ValidationResult.Ok;
            }

            if (containsResult != ValidationResult.Ok)
            {
                return containsResult;
            }

            if (MinContains.HasValue && containsIndices.Count < MinContains)
            {
                return new ValidationResult($"{containsIndices.Count} array items match when at least {MinContains} are expected.");
            }

            if (MaxContains.HasValue && containsIndices.Count > MaxContains)
            {
                return new ValidationResult($"{containsIndices.Count} array items match when at most {MaxContains} are expected.");
            }

            arrayContext.SetEvaluatedIndices(containsIndices);
            return ValidationResult.Ok;
        }
    }
}