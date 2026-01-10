using System.Collections.Frozen;
using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class DependentRequiredValidator : IKeywordValidator
    {
        private readonly FrozenDictionary<string, string[]> _dependentRequiredProperties;

        public string Keyword => "dependentRequired";

        public bool SupportsDirectValidation => true;

        public DependentRequiredValidator(IDictionary<string, IEnumerable<string>> dependentRequiredProperties)
        {
            _dependentRequiredProperties = dependentRequiredProperties.ToFrozenDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray(),
                StringComparer.Ordinal);
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Object)
                return true;

            HashSet<string> propertyNames = new(StringComparer.Ordinal);
            foreach (var prpElement in data.EnumerateObject())
            {
                propertyNames.Add(prpElement.Name);
            }

#pragma warning disable S3267 // Loop has early return for performance
            foreach (var dependency in _dependentRequiredProperties)
            {
                if (propertyNames.Contains(dependency.Key))
                {
                    foreach (var requiredProp in dependency.Value)
                    {
                        if (!propertyNames.Contains(requiredProp))
                            return false;
                    }
                }
            }
#pragma warning restore S3267
            return true;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the dependentRequired keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            HashSet<string> propertyNames = new(StringComparer.Ordinal);
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
