// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 2019-09, Draft 2020-12
// Note: unevaluatedProperties was introduced in Draft 2019-09.
// Validates object properties not evaluated by other keywords.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
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
                var enumerator = context.Data.EnumerateObject();
                while (enumerator.MoveNext())
                {
                    var prpContext = _contextFactory.CreateContextForPropertyFast(context, enumerator.Current.Value);
                    if (!_unevaluatedPropertyValidator.IsValid(prpContext))
                    {
                        return false;
                    }
                }
                return true;
            }

            // With tracking, only validate unevaluated properties
            // Use concrete type access when possible to avoid IEnumerable allocation
            if (objectContext is JsonValidationObjectContext jvoc)
            {
                var annotations = jvoc.GetAnnotations();
                var enumerator = annotations.UnEvaluatedProperties.Values.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var prpContext = _contextFactory.CreateContextForPropertyFast(context, enumerator.Current.Value);
                    if (!_unevaluatedPropertyValidator.IsValid(prpContext))
                    {
                        return false;
                    }
                }
            }
            else if (objectContext is FastValidationObjectContext fvoc)
            {
                var annotations = fvoc.GetAnnotations();
                var enumerator = annotations.UnEvaluatedProperties.Values.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var prpContext = _contextFactory.CreateContextForPropertyFast(context, enumerator.Current.Value);
                    if (!_unevaluatedPropertyValidator.IsValid(prpContext))
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
                // If the instance is not an object, it's considered valid with respect to the unevaluatedProperties keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationObjectContext objectContext)
            {
                throw new InvalidOperationException("Object context is invalid");
            }

            List<ValidationResult>? children = null;
            List<string>? invalidProperties = null;
            List<string>? evaluatedProperties = null;

            // Use concrete type access to avoid IEnumerable allocation
            IEnumerable<JsonProperty> unevaluatedProperties = objectContext switch
            {
                JsonValidationObjectContext jvoc => jvoc.GetAnnotations().UnEvaluatedProperties.Values,
                FastValidationObjectContext fvoc => fvoc.GetAnnotations().UnEvaluatedProperties.Values,
                _ => objectContext.GetUnevaluatedProperties()
            };

            if (unevaluatedProperties is Dictionary<string, JsonProperty>.ValueCollection valueCollection)
            {
                var enumerator = valueCollection.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    ProcessProperty(enumerator.Current);
                }
            }
            else
            {
                // Fallback for other implementations - materialize to array for O(n) instead of Skip/ElementAt O(n²)
                var propsArray = unevaluatedProperties.ToArray();
                for (int i = 0; i < propsArray.Length; i++)
                {
                    ProcessProperty(propsArray[i]);
                }
            }

            void ProcessProperty(JsonProperty prp)
            {
                var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, prp.Value);
                var validationResult = _unevaluatedPropertyValidator.Validate(prpContext, keywordLocation);
                children ??= [];
                children.Add(validationResult);

                if (!validationResult.IsValid)
                {
                    invalidProperties ??= [];
                    invalidProperties.Add(prp.Name);
                }
                else
                {
                    evaluatedProperties ??= [];
                    evaluatedProperties.Add(prp.Name);
                }
            }

            if (invalidProperties != null && invalidProperties.Count > 0)
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

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children is { Count: > 0 } ? children : null };

            // Per spec: annotate with property names that were validated by this keyword
            if (evaluatedProperties is { Count: > 0 })
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
