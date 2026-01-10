using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class AdditionalPropertiesValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _additionalPropertiesSchemaValidator;
        private readonly HashSet<string> _filterPropertyNames;
        private readonly Regex[] _filterPropertyPatternRegexes;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "additionalProperties";

        public AdditionalPropertiesValidator(
            ISchemaValidator additionalPropertiesSchemaValidator,
            IEnumerable<string> filterPropertyNames,
            IEnumerable<string> filterPropertyNamePatterns,
            IJsonValidationContextFactory contextFactory)
        {
            _additionalPropertiesSchemaValidator = additionalPropertiesSchemaValidator;
            _filterPropertyNames = new HashSet<string>(filterPropertyNames, StringComparer.Ordinal);
            _filterPropertyPatternRegexes = filterPropertyNamePatterns
                .Select(EcmaScriptRegexHelper.CreateEcmaScriptRegex)
                .ToArray();
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return true;
            }

            foreach (var prp in context.Data.EnumerateObject())
            {
                if (_filterPropertyNames.Contains(prp.Name)) continue;
                if (MatchesAnyPattern(prp.Name)) continue;

                var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, prp.Value);
                if (!_additionalPropertiesSchemaValidator.IsValid(prpContext))
                {
                    return false;
                }
            }

            return true;
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
            var additionalPropertyNames = new List<string>();

            foreach (var prp in context.Data.EnumerateObject())
            {
                if (_filterPropertyNames.Contains(prp.Name)) continue;
                if (MatchesAnyPattern(prp.Name)) continue;

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
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = additionalPropertyNames }
                };
            }

            return result;
        }

        private bool MatchesAnyPattern(string propertyName)
        {
            return _filterPropertyPatternRegexes.Any(pattern => pattern.IsMatch(propertyName));
        }
    }
}
