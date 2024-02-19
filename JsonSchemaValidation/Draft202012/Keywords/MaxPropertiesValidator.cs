using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Globalization;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MaxPropertiesValidator : IKeywordValidator
    {
        private const string keyword = "maxProperties";
        private readonly int maxProperties;

        public MaxPropertiesValidator(int maxProperties)
        {
            this.maxProperties = maxProperties;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the maxProperties keyword
                return ValidationResult.Ok;
            }

            int numProperties = context.Data.EnumerateObject().Count();
            if (numProperties <= maxProperties)
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }
    }
}