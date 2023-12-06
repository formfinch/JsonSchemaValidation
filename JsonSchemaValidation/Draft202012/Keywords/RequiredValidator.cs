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

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the required keyword
                return ValidationResult.Ok;
            }

            ValidationResult result = new();
            foreach (string propertyName in _propertyNames)
            {
                if(!instance.TryGetProperty(propertyName, out JsonElement value))
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