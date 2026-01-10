using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class MinPropertiesValidator : IKeywordValidator
    {
        private readonly int _minProperties;

        public string Keyword => "minProperties";

        public bool SupportsDirectValidation => true;

        public MinPropertiesValidator(int minProperties)
        {
            _minProperties = minProperties;
        }

        public bool IsValid(JsonElement data) =>
            data.ValueKind != JsonValueKind.Object || data.EnumerateObject().Count() >= _minProperties;

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

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
