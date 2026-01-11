// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that data matches one of multiple type specifications (e.g., "type": ["string", "null"]).

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft201909.Keywords
{
    internal sealed class TypeMultipleTypesValidator : IKeywordValidator
    {
        private readonly IKeywordValidator[] _validators;

        public string Keyword => "type";

        public bool SupportsDirectValidation => true;

        public TypeMultipleTypesValidator(IEnumerable<IKeywordValidator> validators)
        {
            _validators = validators.ToArray();
        }

        public bool IsValid(JsonElement data)
        {
#pragma warning disable S3267 // Loop has early return for performance
            foreach (var validator in _validators)
            {
                if (validator.IsValid(data))
                    return true;
            }
#pragma warning restore S3267
            return false;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            var children = new List<ValidationResult>();
            foreach (var validator in _validators)
            {
                var result = validator.Validate(context, keywordLocation);
                children.Add(result);

                if (result.IsValid)
                {
                    return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children };
                }
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value does not match any of the allowed types") with { Children = children };
        }
    }
}
