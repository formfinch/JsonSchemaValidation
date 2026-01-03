using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeMultipleTypesValidator : IKeywordValidator
    {
        private readonly IEnumerable<IKeywordValidator> _validators;

        public string Keyword => "type";

        public TypeMultipleTypesValidator(IEnumerable<IKeywordValidator> validators)
        {
            _validators = validators;
        }

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
