using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class RequiredValidator : IKeywordValidator
    {
        private readonly IEnumerable<string> _propertyNames;

        public string Keyword => "required";

        public RequiredValidator(IEnumerable<string> propertyNames)
        {
            _propertyNames = propertyNames;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the required keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            var missingProperties = new List<string>();
            foreach (string propertyName in _propertyNames)
            {
                if (!context.Data.TryGetProperty(propertyName, out _))
                {
                    missingProperties.Add(propertyName);
                }
            }

            if (missingProperties.Count > 0)
            {
                var missingList = string.Join(", ", missingProperties.Select(p => $"'{p}'"));
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Missing required properties: {missingList}");
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
