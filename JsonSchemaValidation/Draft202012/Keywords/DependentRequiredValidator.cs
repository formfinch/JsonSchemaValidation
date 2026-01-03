using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

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
            foreach (var dependency in _dependentRequiredProperties)
            {
                if (propertyNames.Contains(dependency.Key))
                {
                    var missingProps = dependency.Value.Where(prpName => !propertyNames.Contains(prpName)).ToList();
                    if (missingProps.Any())
                    {
                        errors.Add($"Property '{dependency.Key}' requires: {string.Join(", ", missingProps.Select(p => $"'{p}'"))}");
                    }
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
