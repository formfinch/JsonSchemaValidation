using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeArrayValidator : IKeywordValidator
    {
        public string Keyword => "type";

        public TypeArrayValidator()
        {
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, "Expected an array");
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
