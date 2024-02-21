using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PatternPropertiesValidator : IKeywordValidator
    {
        private readonly Dictionary<string, ISchemaValidator> _propertySchemaValidators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public PatternPropertiesValidator(Dictionary<string, ISchemaValidator> propertySchemaValidators, IJsonValidationContextFactory contextFactory)
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

            foreach (string propertyNamePattern in _propertySchemaValidators.Keys)
            {
                var validator = _propertySchemaValidators[propertyNamePattern];
                if(validator == null)
                {
                    throw new InvalidOperationException(@"Validator not available for properties pattern: {propertyNamePattern}.");
                }

                // get all properties matching with propertyNamePattern.
                var rxPropertyName = new Regex(propertyNamePattern);
                foreach(var prp in context.Data.EnumerateObject())
                {
                    if (!rxPropertyName.IsMatch(prp.Name)) continue;
                    if (!context.Data.TryGetProperty(prp.Name, out JsonElement value)) continue;

                    if (context is IJsonValidationObjectContext objectContext)
                    {
                        objectContext.MarkPropertyEvaluated(prp.Name);
                    }

                    var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, value);
                    var validationResult = validator.Validate(prpContext);
                    if (validationResult != ValidationResult.Ok)
                    {
                        var result = new ValidationResult($"Property {prp.Name} is invalid");
                        result.Merge(validationResult);
                        return result;
                    }
                }
            }
            return ValidationResult.Ok;
        }
    }
}