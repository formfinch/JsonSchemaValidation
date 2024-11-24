using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinPropertiesValidator : IKeywordValidator
    {
        private const string keyword = "minProperties";
        private readonly int minProperties;

        public MinPropertiesValidator(int minProperties)
        {
            this.minProperties = minProperties;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the minProperties keyword
                return ValidationResult.Ok;
            }

            int numProperties = context.Data.EnumerateObject().Count();
            if (numProperties >= minProperties)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }
    }
}