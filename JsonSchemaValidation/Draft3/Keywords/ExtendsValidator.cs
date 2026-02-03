// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft 3 behavior: extends is similar to allOf in later drafts.
// Validates that data matches the base schema and all extended schemas.

using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class ExtendsValidator : IKeywordValidator
    {
        private readonly List<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;
        private readonly bool _requiresTracking;

        public string Keyword => "extends";

        public ExtendsValidator(List<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
            // Check if any sub-schema requires annotation tracking
            _requiresTracking = _validators.Any(v => v.RequiresAnnotationTracking);
        }

        public bool IsValid(IJsonValidationContext context)
        {
            // Use tracking contexts if any sub-schema needs it, or if parent already tracks
            bool needsTracking = _requiresTracking || context is IJsonValidationObjectContext or IJsonValidationArrayContext;

            // Fast path: short-circuit on first failure
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContextFast(context, needsTracking);
                if (!validator.IsValid(activeContext))
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

            var children = new List<ValidationResult>();
            var contexts = new List<IJsonValidationContext>();
            int idx = 0;
            bool allValid = true;

            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContext(context);
                // Each sub-schema in extends gets path: /extends/0, /extends/1, etc.
                var subSchemaPath = keywordLocation.Append(idx);
                var schemaResult = validator.Validate(activeContext, subSchemaPath);
                children.Add(schemaResult);

                if (!schemaResult.IsValid)
                {
                    allValid = false;
                }
                else
                {
                    contexts.Add(activeContext);
                }
                idx++;
            }

            if (allValid)
            {
                foreach (var activeContext in contexts)
                {
                    _contextFactory.CopyAnnotations(activeContext, context);
                }
            }

            return ValidationResult.Aggregate(instanceLocation, kwLocation, children);
        }
    }
}
