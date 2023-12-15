using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PropertiesValidator : IKeywordValidator
    {
        private readonly Dictionary<string, ISchemaValidator> _propertySchemaValidators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public PropertiesValidator(Dictionary<string, ISchemaValidator> propertySchemaValidators, IJsonValidationContextFactory contextFactory)
        {
            _propertySchemaValidators = propertySchemaValidators;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if(context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return ValidationResult.Ok;
            }

            foreach (string propertyName in _propertySchemaValidators.Keys)
            {
                if (context.Data.TryGetProperty(propertyName, out JsonElement value))
                {
                    var prpContext = _contextFactory.CreateContextForProperty(context, propertyName, value);
                    var validator = _propertySchemaValidators[propertyName];
                    var validationResult = validator.Validate(prpContext);
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