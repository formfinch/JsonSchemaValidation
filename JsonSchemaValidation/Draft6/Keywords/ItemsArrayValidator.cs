// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09
// Note: In Draft 2020-12, this functionality was replaced by "prefixItems".
// Validates array items against tuple schemas when "items" is an array.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
{
    internal sealed class ItemsArrayValidator : IKeywordValidator
    {
        private readonly List<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "items";

        public ItemsArrayValidator(List<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public int TupleSize => _validators.Count;

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            int idx = 0;
            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (idx >= _validators.Count)
                {
                    // Additional items are handled by additionalItems validator
                    break;
                }

                var itemContext = _contextFactory.CreateContextForArrayItemFast(context, item);
                if (!_validators[idx].IsValid(itemContext))
                {
                    return false;
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
            int maxEvaluatedIndex = -1;

            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (idx >= _validators.Count)
                {
                    break;
                }

                var itemContext = _contextFactory.CreateContextForArrayItem(context, idx, item);
                var childKeywordPath = keywordLocation.Append(idx);
                var itemValidationResult = _validators[idx].Validate(itemContext, childKeywordPath);
                children.Add(itemValidationResult);

                if (!itemValidationResult.IsValid)
                {
                    return ValidationResult.Invalid(instanceLocation, kwLocation, $"Item at index {idx.ToString(System.Globalization.CultureInfo.InvariantCulture)} is invalid") with { Children = children };
                }

                arrayContext.SetEvaluatedIndex(idx);
                maxEvaluatedIndex = idx;
                idx++;
            }

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with the largest index validated
            if (maxEvaluatedIndex >= 0)
            {
                return result with
                {
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = maxEvaluatedIndex }
                };
            }

            return result;
        }
    }
}
