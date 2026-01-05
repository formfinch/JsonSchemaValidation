using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PropertiesValidator : IKeywordValidator
    {
        private readonly Dictionary<string, ISchemaValidator> _propertySchemaValidators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "properties";

        public PropertiesValidator(Dictionary<string, ISchemaValidator> propertySchemaValidators, IJsonValidationContextFactory contextFactory)
        {
            _propertySchemaValidators = propertySchemaValidators;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            var children = new List<ValidationResult>();
            var evaluatedProperties = new List<string>();

            foreach (string propertyName in _propertySchemaValidators.Keys)
            {
                if (context.Data.TryGetProperty(propertyName, out JsonElement value))
                {
                    if (context is IJsonValidationObjectContext objectContext)
                    {
                        objectContext.MarkPropertyEvaluated(propertyName);
                    }

                    evaluatedProperties.Add(propertyName);
                    var prpContext = _contextFactory.CreateContextForProperty(context, propertyName, value);
                    var validator = _propertySchemaValidators[propertyName];
                    // Extend keyword path with property name: /properties/propertyName
                    var propertyKeywordPath = keywordLocation.Append(propertyName);
                    var validationResult = validator.Validate(prpContext, propertyKeywordPath);
                    children.Add(validationResult);
                }
            }

            var result = ValidationResult.Aggregate(instanceLocation, kwLocation, children);

            // Per spec: annotate with property names that were validated
            if (result.IsValid && evaluatedProperties.Count > 0)
            {
                return result with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = evaluatedProperties }
                };
            }

            return result;
        }
    }
}
