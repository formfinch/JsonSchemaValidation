using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PropertiesValidator : IKeywordValidator
    {
        private readonly Dictionary<string, ISchemaValidator> _propertySchemaValidators;

        public PropertiesValidator(Dictionary<string, ISchemaValidator> propertySchemaValidators)
        {
            _propertySchemaValidators = propertySchemaValidators;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if(instance.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return ValidationResult.Ok;
            }

            foreach (string propertyName in _propertySchemaValidators.Keys)
            {
                if (instance.TryGetProperty(propertyName, out JsonElement value))
                {
                    var validator = _propertySchemaValidators[propertyName];
                    var validationResult = validator.Validate(value);
                    if(validationResult != ValidationResult.Ok)
                    {
                        var result = new ValidationResult($"Property {propertyName} is invalid");
                        result.Merge(validationResult);
                        return result;
                    }
                }
            }
            return ValidationResult.Ok;
        }
    }
}