using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class UnevaluatedItemsValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _unevaluatedItemValidator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "unevaluatedItems";

        public UnevaluatedItemsValidator(ISchemaValidator unevaluatedItemValidator, IJsonValidationContextFactory contextFactory)
        {
            _unevaluatedItemValidator = unevaluatedItemValidator;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            // If context doesn't support tracking, validate all items conservatively
            if (context is not IJsonValidationArrayContext arrayContext)
            {
                foreach (var item in context.Data.EnumerateArray())
                {
                    var itemContext = _contextFactory.CreateContextForArrayItemFast(context, item);
                    if (!_unevaluatedItemValidator.IsValid(itemContext))
                    {
                        return false;
                    }
                }
                return true;
            }

            // With tracking, only validate unevaluated items
            var annotations = arrayContext.GetAnnotations();
            for (int idx = annotations.EvaluatedIndex + 1; idx < context.Data.GetArrayLength(); idx++)
            {
                if (annotations.ItemsEvaluated || annotations.AdditionalItemsEvaluated || annotations.UnevaluatedItemsEvaluated)
                    break;
                if (annotations.EvaluatedIndices.Contains(idx))
                    continue;

                var item = context.Data[idx];
                var itemContext = _contextFactory.CreateContextForArrayItemFast(context, item);
                if (!_unevaluatedItemValidator.IsValid(itemContext))
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
                // If the instance is not an array, it's considered valid with respect to the unevaluatedItems keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            var children = new List<ValidationResult>();
            var unevaluatedIndices = new List<int>();

            // Get unevaluated indices with their actual array positions
            var annotations = arrayContext.GetAnnotations();
            for (int idx = annotations.EvaluatedIndex + 1; idx < context.Data.GetArrayLength(); idx++)
            {
                if (annotations.ItemsEvaluated || annotations.AdditionalItemsEvaluated || annotations.UnevaluatedItemsEvaluated)
                    break;
                if (annotations.EvaluatedIndices.Contains(idx))
                    continue;

                var item = context.Data[idx];
                var itemContext = _contextFactory.CreateContextForArrayItem(context, idx, item);
                var itemValidationResult = _unevaluatedItemValidator.Validate(itemContext, keywordLocation);
                children.Add(itemValidationResult);

                if (!itemValidationResult.IsValid)
                {
                    return ValidationResult.Invalid(instanceLocation, kwLocation, $"Unevaluated item at index {idx} is invalid") with { Children = children };
                }
                unevaluatedIndices.Add(idx);
            }

            arrayContext.SetUnevaluatedItemsEvaluated();

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with indices that were validated by this keyword
            if (unevaluatedIndices.Count > 0)
            {
                return result with
                {
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = unevaluatedIndices }
                };
            }

            return result;
        }
    }
}
