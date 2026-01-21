// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that an object contains all required properties.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
{
    internal sealed class RequiredValidator : IKeywordValidator
    {
        private readonly string[] _propertyNames;

        public string Keyword => "required";

        public bool SupportsDirectValidation => true;

        public RequiredValidator(IEnumerable<string> propertyNames)
        {
            _propertyNames = propertyNames as string[] ?? propertyNames.ToArray();
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Object)
                return true;

            foreach (string propertyName in _propertyNames)
            {
                if (!data.TryGetProperty(propertyName, out _))
                    return false;
            }
            return true;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

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
                var quotedProps = new List<string>(missingProperties.Count);
                foreach (var p in missingProperties)
                {
                    quotedProps.Add($"'{p}'");
                }
                var missingList = string.Join(", ", quotedProps);
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Missing required properties: {missingList}");
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
