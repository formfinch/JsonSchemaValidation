using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinimumValidator : IKeywordValidator
    {
        private readonly double _minimum;

        public string Keyword => "minimum";

        public MinimumValidator(double minimum)
        {
            _minimum = minimum;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Number)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context.Data.GetDouble() >= _minimum)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value must be at least {_minimum}");
        }
    }
}
