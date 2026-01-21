// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft 3 behavior: Properties validates object properties AND handles required boolean.
// In Draft 3, "required" is a boolean on each property definition, not an array at schema level.

using System.Collections.Frozen;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class PropertiesValidator : IKeywordValidator
    {
        private readonly FrozenDictionary<string, ISchemaValidator> _propertySchemaValidators;
        private readonly FrozenSet<string> _requiredProperties;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "properties";

        public PropertiesValidator(
            Dictionary<string, ISchemaValidator> propertySchemaValidators,
            HashSet<string> requiredProperties,
            IJsonValidationContextFactory contextFactory)
        {
            _propertySchemaValidators = propertySchemaValidators.ToFrozenDictionary(StringComparer.Ordinal);
            _requiredProperties = requiredProperties.ToFrozenSet(StringComparer.Ordinal);
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return true;
            }

            // Check required properties first (Draft 3: required is boolean on property definition)
            foreach (var requiredProp in _requiredProperties)
            {
                if (!context.Data.TryGetProperty(requiredProp, out _))
                {
                    return false;
                }
            }

            // Validate property schemas
            foreach (string propertyName in _propertySchemaValidators.Keys)
            {
                if (context.Data.TryGetProperty(propertyName, out JsonElement value))
                {
                    var prpContext = _contextFactory.CreateContextForPropertyFast(context, value);
                    var validator = _propertySchemaValidators[propertyName];
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
            var evaluatedProperties = new List<string>();
            var errors = new List<string>();

            // Check required properties first (Draft 3: required is boolean on property definition)
            foreach (var requiredProp in _requiredProperties)
            {
                if (!context.Data.TryGetProperty(requiredProp, out _))
                {
                    errors.Add($"Missing required property: '{requiredProp}'");
                }
            }

            // Validate property schemas
            foreach (string propertyName in _propertySchemaValidators.Keys)
            {
                if (context.Data.TryGetProperty(propertyName, out JsonElement value))
                {
                    if (context is IJsonValidationObjectContext objectContext)
                    {
                        objectContext.MarkPropertyEvaluated(propertyName);
                    }

                    evaluatedProperties.Add(propertyName);
                    var prpContext = _contextFactory.CreateContextForProperty(context, propertyName, value);
                    var validator = _propertySchemaValidators[propertyName];
                    // Extend keyword path with property name: /properties/propertyName
                    var propertyKeywordPath = keywordLocation.Append(propertyName);
                    var validationResult = validator.Validate(prpContext, propertyKeywordPath);
                    children.Add(validationResult);
                }
            }

            // If there are required property errors
            if (errors.Count > 0)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, string.Join("; ", errors)) with { Children = children.Count > 0 ? children : null };
            }

            var aggregatedResult = ValidationResult.Aggregate(instanceLocation, kwLocation, children);

            // Per spec: annotate with property names that were validated
            if (aggregatedResult.IsValid && evaluatedProperties.Count > 0)
            {
                return aggregatedResult with
                {
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = evaluatedProperties }
                };
            }

            return aggregatedResult;
        }
    }
}
