// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09
// Note: In Draft 2020-12, "additionalItems" was removed and "items" handles this case.
// Validates array items beyond those covered by the "items" array (tuple validation).

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft4.Keywords
{
    internal sealed class AdditionalItemsValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly int _tupleSize;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "additionalItems";

        public AdditionalItemsValidator(ISchemaValidator validator, int tupleSize, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _tupleSize = tupleSize;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            int idx = 0;
            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (idx >= _tupleSize)
                {
                    var itemContext = _contextFactory.CreateContextForArrayItemFast(context, item);
                    if (!_validator.IsValid(itemContext))
                    {
                        return false;
                    }
                }
                idx++;
            }

            return true;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            var children = new List<ValidationResult>();
            int idx = 0;
            bool validatedAnyItems = false;

            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (idx >= _tupleSize)
                {
                    validatedAnyItems = true;
                    var itemContext = _contextFactory.CreateContextForArrayItem(context, idx, item);
                    var itemValidationResult = _validator.Validate(itemContext, keywordLocation);
                    children.Add(itemValidationResult);

                    if (!itemValidationResult.IsValid)
                    {
                        return ValidationResult.Invalid(instanceLocation, kwLocation, $"Additional item at index {idx.ToString(System.Globalization.CultureInfo.InvariantCulture)} is invalid") with { Children = children };
                    }

                    arrayContext.SetEvaluatedIndex(idx);
                }
                idx++;
            }

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with true if additionalItems keyword validated any items
            if (validatedAnyItems)
            {
                return result with
                {
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = true }
                };
            }

            return result;
        }
    }
}
