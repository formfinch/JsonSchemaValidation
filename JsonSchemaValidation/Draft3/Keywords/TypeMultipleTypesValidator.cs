// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft 3 behavior: Validates that data matches one of multiple type specifications or schemas.
// In Draft 3, the type array can contain both type strings AND schemas.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class TypeMultipleTypesValidator : IKeywordValidator
    {
        private readonly List<IKeywordValidator> _typeValidators;
        private readonly List<ISchemaValidator> _schemaValidators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "type";

        public bool SupportsDirectValidation => _schemaValidators.Count == 0;

        public TypeMultipleTypesValidator(
            List<IKeywordValidator> typeValidators,
            List<ISchemaValidator> schemaValidators,
            IJsonValidationContextFactory contextFactory)
        {
            _typeValidators = typeValidators;
            _schemaValidators = schemaValidators;
            _contextFactory = contextFactory;
        }

        public bool IsValid(JsonElement data)
        {
            // Fast path for type-only validation
            foreach (var validator in _typeValidators)
            {
                if (validator.IsValid(data))
                    return true;
            }
            return false;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            // Check type validators first
            foreach (var validator in _typeValidators)
            {
                if (validator.IsValid(context.Data))
                    return true;
            }

            // Check schema validators
            foreach (var validator in _schemaValidators)
            {
                var activeContext = _contextFactory.CreateFreshContextFast(context, false);
                if (validator.IsValid(activeContext))
                    return true;
            }

            return false;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            var children = new List<ValidationResult>();

            // Check type validators first
            foreach (var validator in _typeValidators)
            {
                var result = validator.Validate(context, keywordLocation);
                children.Add(result);

                if (result.IsValid)
                {
                    return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children };
                }
            }

            // Check schema validators
            int idx = 0;
            foreach (var validator in _schemaValidators)
            {
                var activeContext = _contextFactory.CreateFreshContext(context);
                var subSchemaPath = keywordLocation.Append(idx);
                var schemaResult = validator.Validate(activeContext, subSchemaPath);
                children.Add(schemaResult);

                if (schemaResult.IsValid)
                {
                    _contextFactory.CopyAnnotations(activeContext, context);
                    return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children };
                }
                idx++;
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value does not match any of the allowed types or schemas") with { Children = children };
        }
    }
}
