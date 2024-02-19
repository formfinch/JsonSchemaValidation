using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Data;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class DependentSchemasValidator : IKeywordValidator
    {
        private readonly IDictionary<string, ISchemaValidator> _dependentSchemasProperties;


        public DependentSchemasValidator(IDictionary<string, ISchemaValidator> dependentSchemasProperties)
        {
            _dependentSchemasProperties = dependentSchemasProperties;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the dependentRequired keyword
                return ValidationResult.Ok;
            }

            HashSet<string> propertyNames = new();
            foreach(var prpElement in context.Data.EnumerateObject())
            {
                propertyNames.Add(prpElement.Name);
            }

            ValidationResult result = new();
            foreach (var dependency in _dependentSchemasProperties)
            {
                if(propertyNames.Contains(dependency.Key))
                {
                    var validator = dependency.Value;
                    var validationResult = validator.Validate(context);
                    result.Merge(validationResult);
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