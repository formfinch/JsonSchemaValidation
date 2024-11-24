using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class DependentRequiredValidator : IKeywordValidator
    {
        private readonly IDictionary<string, IEnumerable<string>> _dependentRequiredProperties;

        public DependentRequiredValidator(IDictionary<string, IEnumerable<string>> dependentRequiredProperties)
        {
            _dependentRequiredProperties = dependentRequiredProperties;
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
            foreach (var dependency in _dependentRequiredProperties)
            {
                if(propertyNames.Contains(dependency.Key) 
                    && dependency.Value.Any(prpName => !propertyNames.Contains(prpName)))
                {
                    result.AddError($"Property {dependency.Key} has missing required properties.");

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