using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeBooleanValidator : IKeywordValidator
    {
        public string Keyword => "type";

        public TypeBooleanValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.False
                && context.Data.ValueKind != JsonValueKind.True)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Expected a boolean value");
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
