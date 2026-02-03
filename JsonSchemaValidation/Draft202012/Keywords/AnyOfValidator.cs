// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class AnyOfValidator : IKeywordValidator
    {
        private readonly List<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;
        private readonly bool _requiresTracking;

        public string Keyword => "anyOf";

        public AnyOfValidator(List<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
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

            // Fast path: short-circuit on first success
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContextFast(context, needsTracking);
                if (validator.IsValid(activeContext))
                {
                    return true;
                }
            }

            return false;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            // Pre-size based on validator count, lazy init for contexts
            List<IJsonValidationContext>? contexts = null;
            var children = new List<ValidationResult>(_validators.Count);
            bool anyValid = false;

            int index = 0;
            foreach (var validator in _validators)
            {
                var activeContext = _contextFactory.CreateFreshContext(context);
                var childKeywordPath = keywordLocation.Append(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                var childResult = validator.Validate(activeContext, childKeywordPath);
                children.Add(childResult);

                if (childResult.IsValid)
                {
                    contexts ??= new List<IJsonValidationContext>(_validators.Count);
                    contexts.Add(activeContext);
                    anyValid = true;
                }
                index++;
            }

            if (anyValid)
            {
                if (contexts != null)
                {
                    foreach (var activeContext in contexts)
                    {
                        _contextFactory.CopyAnnotations(activeContext, context);
                    }
                }
                return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children };
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Instance did not validate against any of the schemas in 'anyOf'") with { Children = children };
        }
    }
}
