// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that data matches one of multiple type specifications (e.g., "type": ["string", "null"]).

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
{
    internal sealed class TypeMultipleTypesValidator : IKeywordValidator
    {
        private readonly List<IKeywordValidator> _validators;

        public string Keyword => "type";

        public bool SupportsDirectValidation => true;

        public TypeMultipleTypesValidator(List<IKeywordValidator> validators)
        {
            _validators = validators;
        }

        public bool IsValid(JsonElement data)
        {
            foreach (var validator in _validators)
            {
                if (validator.IsValid(data))
                    return true;
            }
            return false;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            var children = new List<ValidationResult>();
            foreach (var validator in _validators)
            {
                var result = validator.Validate(context, keywordLocation);
                children.Add(result);

                if (result.IsValid)
                {
                    return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children };
                }
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value does not match any of the allowed types") with { Children = children };
        }
    }
}
