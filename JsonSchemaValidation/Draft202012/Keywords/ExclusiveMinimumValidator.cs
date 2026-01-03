using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ExclusiveMinimumValidator : IKeywordValidator
    {
        private readonly double _minimum;

        public string Keyword => "exclusiveMinimum";

        public ExclusiveMinimumValidator(double minimum)
        {
            _minimum = minimum;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Number)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (context.Data.GetDouble() > _minimum)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value must be greater than {_minimum}");
        }
    }
}
