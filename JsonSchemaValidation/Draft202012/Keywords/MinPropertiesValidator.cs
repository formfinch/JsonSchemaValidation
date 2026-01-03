using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinPropertiesValidator : IKeywordValidator
    {
        private readonly int _minProperties;

        public string Keyword => "minProperties";

        public MinPropertiesValidator(int minProperties)
        {
            _minProperties = minProperties;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the minProperties keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            int numProperties = context.Data.EnumerateObject().Count();
            if (numProperties >= _minProperties)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Object has {numProperties} properties, which is less than the minimum of {_minProperties}");
        }
    }
}
