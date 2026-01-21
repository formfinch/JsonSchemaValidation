// Draft 7 behavior: Validates that at least one array item matches the given schema.
// Note: minContains and maxContains were added in Draft 2019-09 and are NOT supported in Draft 7.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
{
    internal sealed class ContainsValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "contains";

        public ContainsValidator(ISchemaValidator validator, IJsonValidationContextFactory contextFactory)
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

            // Draft 7: simple "at least one" check
            foreach (var item in context.Data.EnumerateArray())
            {
                var itemContext = _contextFactory.CreateContextForArrayItemFast(context, item);
                if (_validator.IsValid(itemContext))
                {
                    return true;
                }
            }

            return false;
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

            if (containsIndices.Count == 0)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, "No array item matches the 'contains' schema") with { Children = children };
            }

            // Per spec: annotate with indices of items that matched
            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Children = children,
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = containsIndices }
            };
        }
    }
}
