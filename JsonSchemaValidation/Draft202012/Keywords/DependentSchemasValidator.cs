using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class DependentSchemasValidator : IKeywordValidator
    {
        private readonly IDictionary<string, ISchemaValidator> _dependentSchemasProperties;

        public string Keyword => "dependentSchemas";

        public DependentSchemasValidator(IDictionary<string, ISchemaValidator> dependentSchemasProperties)
        {
            _dependentSchemasProperties = dependentSchemasProperties;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the dependentSchemas keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            HashSet<string> propertyNames = new();
            foreach (var prpElement in context.Data.EnumerateObject())
            {
                propertyNames.Add(prpElement.Name);
            }

            var children = new List<ValidationResult>();
            var failedProperties = new List<string>();

            foreach (var dependency in _dependentSchemasProperties)
            {
                if (propertyNames.Contains(dependency.Key))
                {
                    var validator = dependency.Value;
                    var childKeywordPath = keywordLocation.Append(dependency.Key);
                    var validationResult = validator.Validate(context, childKeywordPath);
                    children.Add(validationResult);

                    if (!validationResult.IsValid)
                    {
                        failedProperties.Add(dependency.Key);
                    }
                }
            }

            if (failedProperties.Count > 0)
            {
                var props = string.Join(", ", failedProperties.Select(p => $"'{p}'"));
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Dependent schema validation failed for properties: {props}") with { Children = children };
            }

            return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };
        }
    }
}
