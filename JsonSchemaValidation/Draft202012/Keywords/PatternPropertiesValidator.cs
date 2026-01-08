using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PatternPropertiesValidator : IKeywordValidator
    {
        private readonly Dictionary<Regex, ISchemaValidator> _propertySchemaValidators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "patternProperties";

        public PatternPropertiesValidator(Dictionary<Regex, ISchemaValidator> propertySchemaValidators, IJsonValidationContextFactory contextFactory)
        {
            _propertySchemaValidators = propertySchemaValidators;
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

            var children = new List<ValidationResult>();
            var matchedProperties = new List<string>();

            foreach (var kvp in _propertySchemaValidators)
            {
                var rxPropertyName = kvp.Key;
                var validator = kvp.Value;
                if (validator == null)
                {
                    throw new InvalidOperationException(@"Validator not available for properties pattern.");
                }

                // get all properties matching with propertyNamePattern.
#pragma warning disable S3267 // Loop has side effects (validation calls, early return)
                foreach (var prp in context.Data.EnumerateObject())
#pragma warning restore S3267
                {
                    if (!rxPropertyName.IsMatch(prp.Name)) continue;
                    if (!context.Data.TryGetProperty(prp.Name, out JsonElement value)) continue;

                    if (context is IJsonValidationObjectContext objectContext)
                    {
                        objectContext.MarkPropertyEvaluated(prp.Name);
                    }

                    if (!matchedProperties.Contains(prp.Name))
                    {
                        matchedProperties.Add(prp.Name);
                    }

                    var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, value);
                    var childKeywordPath = keywordLocation.Append(rxPropertyName.ToString());
                    var validationResult = validator.Validate(prpContext, childKeywordPath);
                    children.Add(validationResult);

                    if (!validationResult.IsValid)
                    {
                        return ValidationResult.Invalid(instanceLocation, kwLocation, $"Property '{prp.Name}' does not match pattern schema") with { Children = children };
                    }
                }
            }

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with property names that matched patterns
            if (matchedProperties.Count > 0)
            {
                return result with
                {
                    Annotations = new Dictionary<string, object?> { [Keyword] = matchedProperties }
                };
            }

            return result;
        }
    }
}
