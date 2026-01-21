// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates object properties whose names match the specified patterns.

using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
{
    internal sealed class PatternPropertiesValidator : IKeywordValidator
    {
        private readonly Dictionary<Regex, ISchemaValidator> _propertySchemaValidators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "patternProperties";

        public PatternPropertiesValidator(Dictionary<Regex, ISchemaValidator> propertySchemaValidators, IJsonValidationContextFactory contextFactory)
        {
            _propertySchemaValidators = propertySchemaValidators;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                return true;
            }

            foreach (var kvp in _propertySchemaValidators)
            {
                var rxPropertyName = kvp.Key;
                var validator = kvp.Value;

                foreach (var prp in context.Data.EnumerateObject())
                {
                    if (!rxPropertyName.IsMatch(prp.Name)) continue;

                    var prpContext = _contextFactory.CreateContextForPropertyFast(context, prp.Value);
                    if (!validator.IsValid(prpContext))
                    {
                        return false;
                    }
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

                    if (!matchedProperties.Contains(prp.Name, StringComparer.Ordinal))
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
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = matchedProperties }
                };
            }

            return result;
        }
    }
}
