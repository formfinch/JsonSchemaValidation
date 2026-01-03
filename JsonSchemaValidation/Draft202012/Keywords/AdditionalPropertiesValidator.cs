using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
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

        public string Keyword => "additionalProperties";

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

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            IEnumerable<Regex> propertyNamePatternMatchers = Array.Empty<Regex>();
            if (_filterPropertyNamePatterns.Any())
            {
                propertyNamePatternMatchers = _filterPropertyNamePatterns.Select(pattern => EcmaScriptRegexHelper.CreateEcmaScriptRegex(pattern));
            }

            var children = new List<ValidationResult>();
            var additionalPropertyNames = new List<string>();

            foreach (var prp in context.Data.EnumerateObject())
            {
                if (_filterPropertyNames.Any() && _filterPropertyNames.Contains(prp.Name)) continue;
                if (propertyNamePatternMatchers.Any() && propertyNamePatternMatchers.Any(pattern => pattern.IsMatch(prp.Name))) continue;

                var validator = _additionalPropertiesSchemaValidator;
                if (validator == null)
                {
                    throw new InvalidOperationException(@"Validator not available for additional properties.");
                }

                if (context is JsonValidationObjectContext objectcontext)
                {
                    objectcontext.MarkPropertyEvaluated(prp.Name);
                }

                additionalPropertyNames.Add(prp.Name);
                var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, prp.Value);
                var validationResult = validator.Validate(prpContext, keywordLocation);
                children.Add(validationResult);

                if (!validationResult.IsValid)
                {
                    return ValidationResult.Invalid(instanceLocation, kwLocation, $"Additional property '{prp.Name}' is invalid") with { Children = children };
                }
            }

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with property names that were validated as additional
            if (additionalPropertyNames.Count > 0)
            {
                return result with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = additionalPropertyNames }
                };
            }

            return result;
        }
    }
}
