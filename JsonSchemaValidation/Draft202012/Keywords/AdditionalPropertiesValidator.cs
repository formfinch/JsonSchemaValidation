using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class AdditionalPropertiesValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _additionalPropertiesSchemaValidator;
        private readonly IEnumerable<string> _filterPropertyNames;
        private readonly IEnumerable<string> _filterPropertyNamePatterns;
        private readonly IJsonValidationContextFactory _contextFactory;

        public AdditionalPropertiesValidator(
            ISchemaValidator additionalPropertiesSchemaValidator, 
            IEnumerable<string> filterPropertyNames,
            IEnumerable<string> filterPropertyNamePatterns,
            IJsonValidationContextFactory contextFactory)
        {
            _additionalPropertiesSchemaValidator = additionalPropertiesSchemaValidator;
            _filterPropertyNames = filterPropertyNames;
            _filterPropertyNamePatterns = filterPropertyNamePatterns;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if(context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return ValidationResult.Ok;
            }

            IEnumerable<Regex> propertyNamePatternMatchers = Array.Empty<Regex>();
            if (_filterPropertyNamePatterns.Any())
            {
                propertyNamePatternMatchers = _filterPropertyNamePatterns.Select(pattern => new Regex(pattern));
            }

            foreach (var prp in context.Data.EnumerateObject())
            {
                if (_filterPropertyNames.Any() && _filterPropertyNames.Contains(prp.Name)) continue;
                if (propertyNamePatternMatchers.Any() && propertyNamePatternMatchers.Any(pattern => pattern.IsMatch(prp.Name))) continue;

                var validator = _additionalPropertiesSchemaValidator;
                if(validator == null)
                {
                    throw new InvalidOperationException(@"Validator not available for additional properties.");
                }

                var validationResult = validator.Validate(context);
                if (validationResult != ValidationResult.Ok)
                {
                    var result = new ValidationResult($"Property {prp.Name} is invalid");
                    result.Merge(validationResult);
                    return result;
                }
            }
            return ValidationResult.Ok;
        }
    }
}