using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PrefixItemsValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "prefixItems";

        public PrefixItemsValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the prefixItems keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (!_validators.Any())
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            var children = new List<ValidationResult>();
            int prefixItemIndex = 0;
            int arrayLength = context.Data.GetArrayLength();
            int schemaCount = _validators.Count();

            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (prefixItemIndex >= schemaCount)
                    break;

                var validator = _validators.ElementAt(prefixItemIndex);
                var itemContext = _contextFactory.CreateContextForArrayItem(context, prefixItemIndex, item);
                var childKeywordPath = keywordLocation.Append(prefixItemIndex.ToString());
                var itemValidationResult = validator.Validate(itemContext, childKeywordPath);
                children.Add(itemValidationResult);

                if (!itemValidationResult.IsValid)
                {
                    return ValidationResult.Invalid(instanceLocation, kwLocation, $"Item at index {prefixItemIndex} is invalid") with { Children = children };
                }
                arrayContext.SetEvaluatedIndex(prefixItemIndex);
                prefixItemIndex++;
            }

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with largest index validated, or true if array length <= schema count
            if (prefixItemIndex > 0)
            {
                object annotationValue = arrayLength <= schemaCount ? true : prefixItemIndex - 1;
                return result with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = annotationValue }
                };
            }

            return result;
        }
    }
}
