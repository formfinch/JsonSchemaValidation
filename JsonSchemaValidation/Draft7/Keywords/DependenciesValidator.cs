// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft 7 behavior: The "dependencies" keyword combines both property dependencies (arrays)
// and schema dependencies (objects/booleans) in a single keyword.
// In Draft 2019-09+, this was split into "dependentRequired" and "dependentSchemas".

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
{
    internal sealed class DependenciesValidator : IKeywordValidator
    {
        private readonly Dictionary<string, string[]> _propertyDependencies;
        private readonly Dictionary<string, ISchemaValidator> _schemaDependencies;

        public string Keyword => "dependencies";

        public DependenciesValidator(
            Dictionary<string, string[]> propertyDependencies,
            Dictionary<string, ISchemaValidator> schemaDependencies)
        {
            _propertyDependencies = propertyDependencies;
            _schemaDependencies = schemaDependencies;
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

            // Check property dependencies (arrays)
            foreach (var dependency in _propertyDependencies)
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

            // Check schema dependencies (objects/booleans)
            foreach (var dependency in _schemaDependencies)
            {
                if (propertyNames.Contains(dependency.Key) && !dependency.Value.IsValid(context))
                    return false;
            }

            return true;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            HashSet<string> propertyNames = new(StringComparer.Ordinal);
            foreach (var prpElement in context.Data.EnumerateObject())
            {
                propertyNames.Add(prpElement.Name);
            }

            var errors = new List<string>();
            var children = new List<ValidationResult>();

            // Validate property dependencies (arrays)
            foreach (var dependency in _propertyDependencies)
            {
                if (!propertyNames.Contains(dependency.Key))
                    continue;

                var missingProps = new List<string>();
                foreach (var prpName in dependency.Value)
                {
                    if (!propertyNames.Contains(prpName))
                        missingProps.Add($"'{prpName}'");
                }
                if (missingProps.Count > 0)
                {
                    var missingList = string.Join(", ", missingProps);
                    errors.Add($"Property '{dependency.Key}' requires: {missingList}");
                }
            }

            // Validate schema dependencies (objects/booleans)
            var failedSchemaProperties = new List<string>();
            foreach (var dependency in _schemaDependencies)
            {
                if (!propertyNames.Contains(dependency.Key))
                    continue;

                var validator = dependency.Value;
                var childKeywordPath = keywordLocation.Append(dependency.Key);
                var validationResult = validator.Validate(context, childKeywordPath);
                children.Add(validationResult);

                if (!validationResult.IsValid)
                {
                    failedSchemaProperties.Add(dependency.Key);
                }
            }

            if (failedSchemaProperties.Count > 0)
            {
                var propsList = new List<string>(failedSchemaProperties.Count);
                foreach (var p in failedSchemaProperties)
                {
                    propsList.Add($"'{p}'");
                }
                var props = string.Join(", ", propsList);
                errors.Add($"Schema dependency validation failed for properties: {props}");
            }

            if (errors.Count > 0)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, string.Join("; ", errors)) with { Children = children.Count > 0 ? children : null };
            }

            return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };
        }
    }
}
