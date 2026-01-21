// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 2019-09, Draft 2020-12
// Note: In Draft 6 and Draft 7, this functionality was part of the "dependencies" keyword.
// Validates schema dependencies when a property is present.

using System.Collections.Frozen;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    internal sealed class DependentSchemasValidator : IKeywordValidator
    {
        private readonly FrozenDictionary<string, ISchemaValidator> _dependentSchemasProperties;

        public string Keyword => "dependentSchemas";

        public DependentSchemasValidator(IDictionary<string, ISchemaValidator> dependentSchemasProperties)
        {
            _dependentSchemasProperties = dependentSchemasProperties.ToFrozenDictionary(StringComparer.Ordinal);
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
                return true;

            HashSet<string> propertyNames = new(StringComparer.Ordinal);
            foreach (var prpElement in context.Data.EnumerateObject())
            {
                propertyNames.Add(prpElement.Name);
            }

#pragma warning disable S3267, S1066 // Loop has early return for performance
            foreach (var dependency in _dependentSchemasProperties)
            {
                if (propertyNames.Contains(dependency.Key) && !dependency.Value.IsValid(context))
                    return false;
            }
#pragma warning restore S3267, S1066

            return true;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the dependentSchemas keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            HashSet<string> propertyNames = new(StringComparer.Ordinal);
            foreach (var prpElement in context.Data.EnumerateObject())
            {
                propertyNames.Add(prpElement.Name);
            }

            var children = new List<ValidationResult>();
            var failedProperties = new List<string>();

            foreach (var dependency in _dependentSchemasProperties)
            {
                if (!propertyNames.Contains(dependency.Key))
                    continue;

                var validator = dependency.Value;
                var childKeywordPath = keywordLocation.Append(dependency.Key);
                var validationResult = validator.Validate(context, childKeywordPath);
                children.Add(validationResult);

                if (!validationResult.IsValid)
                {
                    failedProperties.Add(dependency.Key);
                }
            }

            if (failedProperties.Count > 0)
            {
                var propsList = new List<string>(failedProperties.Count);
                foreach (var p in failedProperties)
                {
                    propsList.Add($"'{p}'");
                }
                var props = string.Join(", ", propsList);
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Dependent schema validation failed for properties: {props}") with { Children = children };
            }

            return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };
        }
    }
}
