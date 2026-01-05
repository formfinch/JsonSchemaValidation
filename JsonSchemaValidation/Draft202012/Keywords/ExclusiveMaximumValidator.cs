using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ExclusiveMaximumValidator : IKeywordValidator
    {
        private readonly double _maximum;

        public string Keyword => "exclusiveMaximum";

        public ExclusiveMaximumValidator(double maximum)
        {
            _maximum = maximum;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Number)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (context.Data.GetDouble() < _maximum)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value must be less than {_maximum}");
        }
    }
}
