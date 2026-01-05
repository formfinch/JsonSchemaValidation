using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MaxPropertiesValidator : IKeywordValidator
    {
        private readonly int _maxProperties;

        public string Keyword => "maxProperties";

        public MaxPropertiesValidator(int maxProperties)
        {
            _maxProperties = maxProperties;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the maxProperties keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            int numProperties = context.Data.EnumerateObject().Count();
            if (numProperties <= _maxProperties)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Object has {numProperties} properties, which exceeds the maximum of {_maxProperties}");
        }
    }
}
