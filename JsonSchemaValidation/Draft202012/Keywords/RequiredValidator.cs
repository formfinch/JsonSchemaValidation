using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class RequiredValidator : IKeywordValidator
    {
        private readonly IEnumerable<string> _propertyNames;

        public RequiredValidator(IEnumerable<string> propertyNames)
        {
            _propertyNames = propertyNames;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the required keyword
                return ValidationResult.Ok;
            }

            ValidationResult result = new();
            foreach (string propertyName in _propertyNames)
            {
                if(!context.Data.TryGetProperty(propertyName, out JsonElement value))
                {
                    result.AddError($"Missing property: {propertyName}");
                }
            }

            if(!result.IsValid)
            {
                return result;
            }

            return ValidationResult.Ok;
        }
    }
}