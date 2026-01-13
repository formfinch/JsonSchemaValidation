// Draft behavior: Identical in Draft 3, Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that the data is a JSON number.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class TypeNumberValidator : IKeywordValidator
    {
        public static readonly TypeNumberValidator Instance = new();

        public string Keyword => "type";

        public bool SupportsDirectValidation => true;

        private TypeNumberValidator() { }

        public bool IsValid(JsonElement data) => data.ValueKind == JsonValueKind.Number;

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Expected a number");
        }
    }
}
