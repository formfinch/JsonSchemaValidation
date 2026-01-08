using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class DependentRequiredValidator : IKeywordValidator
    {
        private readonly IDictionary<string, IEnumerable<string>> _dependentRequiredProperties;

        public string Keyword => "dependentRequired";

        public DependentRequiredValidator(IDictionary<string, IEnumerable<string>> dependentRequiredProperties)
        {
            _dependentRequiredProperties = dependentRequiredProperties;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the dependentRequired keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            HashSet<string> propertyNames = new();
            foreach (var prpElement in context.Data.EnumerateObject())
            {
                propertyNames.Add(prpElement.Name);
            }

            var errors = new List<string>();
            foreach (var dependency in _dependentRequiredProperties.Where(d => propertyNames.Contains(d.Key)))
            {
                var missingProps = dependency.Value.Where(prpName => !propertyNames.Contains(prpName));
                var missingList = string.Join(", ", missingProps.Select(p => $"'{p}'"));
                if (missingList.Length > 0)
                {
                    errors.Add($"Property '{dependency.Key}' requires: {missingList}");
                }
            }

            if (errors.Count > 0)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, string.Join("; ", errors));
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
