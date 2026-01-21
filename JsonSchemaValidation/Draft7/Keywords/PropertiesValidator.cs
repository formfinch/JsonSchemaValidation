// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that object properties match their corresponding schemas.

using System.Collections.Frozen;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
{
    internal sealed class PropertiesValidator : IKeywordValidator
    {
        private readonly FrozenDictionary<string, ISchemaValidator> _propertySchemaValidators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "properties";

        public PropertiesValidator(Dictionary<string, ISchemaValidator> propertySchemaValidators, IJsonValidationContextFactory contextFactory)
        {
            _propertySchemaValidators = propertySchemaValidators.ToFrozenDictionary(StringComparer.Ordinal);
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return true;
            }

            foreach (string propertyName in _propertySchemaValidators.Keys)
            {
                if (context.Data.TryGetProperty(propertyName, out JsonElement value))
                {
                    var prpContext = _contextFactory.CreateContextForPropertyFast(context, value);
                    var validator = _propertySchemaValidators[propertyName];
                    if (!validator.IsValid(prpContext))
                    {
                        return false;
                    }
                }
            }

            return true;
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
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = evaluatedProperties }
                };
            }

            return result;
        }
    }
}
