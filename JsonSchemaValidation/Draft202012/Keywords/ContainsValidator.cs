using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ContainsValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "contains";

        public int? MinContains { get; set; }
        public int? MaxContains { get; set; }

        public ContainsValidator(ISchemaValidator validator, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the contains keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            var children = new List<ValidationResult>();
            int idx = 0;
            var containsIndices = new List<int>();
            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                var itemContext = _contextFactory.CreateContextForArrayItem(context, idx, item);
                var itemValidationResult = _validator.Validate(itemContext, keywordLocation);
                children.Add(itemValidationResult);

                if (itemValidationResult.IsValid)
                {
                    containsIndices.Add(idx);
                }
                idx++;
            }

            if (MinContains.HasValue && MinContains.Value == 0 && containsIndices.Count == 0)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children };
            }

            if (containsIndices.Count == 0)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, "No array item matches with the 'contains' schema") with { Children = children };
            }

            if (MinContains.HasValue && containsIndices.Count < MinContains)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"{containsIndices.Count} array items match when at least {MinContains} are expected") with { Children = children };
            }

            if (MaxContains.HasValue && containsIndices.Count > MaxContains)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"{containsIndices.Count} array items match when at most {MaxContains} are expected") with { Children = children };
            }

            arrayContext.SetEvaluatedIndices(containsIndices);

            // Per spec: annotate with indices of items that matched
            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Children = children,
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = containsIndices }
            };
        }
    }
}
