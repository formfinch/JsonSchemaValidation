// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class UnevaluatedPropertiesValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _unevaluatedPropertyValidator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "unevaluatedProperties";

        public UnevaluatedPropertiesValidator(ISchemaValidator unevaluatedPropertyValidator, IJsonValidationContextFactory contextFactory)
        {
            _unevaluatedPropertyValidator = unevaluatedPropertyValidator;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                return true;
            }

            // If context doesn't support tracking, validate all properties conservatively
            if (context is not IJsonValidationObjectContext objectContext)
            {
                foreach (var prp in context.Data.EnumerateObject())
                {
                    var prpContext = _contextFactory.CreateContextForPropertyFast(context, prp.Value);
                    if (!_unevaluatedPropertyValidator.IsValid(prpContext))
                    {
                        return false;
                    }
                }
                return true;
            }

            // With tracking, only validate unevaluated properties
            var unevaluatedProps = objectContext.GetUnevaluatedProperties();
            for (int i = 0; unevaluatedProps.Skip(i).Any(); i++)
            {
                var prp = unevaluatedProps.ElementAt(i);
                var prpContext = _contextFactory.CreateContextForPropertyFast(context, prp.Value);
                if (!_unevaluatedPropertyValidator.IsValid(prpContext))
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
                // If the instance is not an object, it's considered valid with respect to the unevaluatedProperties keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationObjectContext objectContext)
            {
                throw new InvalidOperationException("Object context is invalid");
            }

            var children = new List<ValidationResult>();
            var invalidProperties = new List<string>();
            var evaluatedProperties = new List<string>();

            var unevaluatedProps = objectContext.GetUnevaluatedProperties();
            for (int i = 0; unevaluatedProps.Skip(i).Any(); i++)
            {
                var prp = unevaluatedProps.ElementAt(i);
                var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, prp.Value);
                var validationResult = _unevaluatedPropertyValidator.Validate(prpContext, keywordLocation);
                children.Add(validationResult);

                if (!validationResult.IsValid)
                {
                    invalidProperties.Add(prp.Name);
                }
                else
                {
                    evaluatedProperties.Add(prp.Name);
                }
            }

            if (invalidProperties.Count > 0)
            {
                var quotedProps = new List<string>(invalidProperties.Count);
                foreach (var p in invalidProperties)
                {
                    quotedProps.Add($"'{p}'");
                }
                var props = string.Join(", ", quotedProps);
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Unevaluated properties are invalid: {props}") with { Children = children };
            }

            objectContext.SetUnevaluatedPropertiesEvaluated();

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with property names that were validated by this keyword
            if (evaluatedProperties.Count > 0)
            {
                return result with
                {
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = evaluatedProperties }
                };
            }

            return result;
        }
    }
}
