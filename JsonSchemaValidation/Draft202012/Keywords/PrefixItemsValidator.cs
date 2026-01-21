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
    internal sealed class PrefixItemsValidator : IKeywordValidator
    {
        private readonly IEnumerable<ISchemaValidator> _validators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "prefixItems";

        public PrefixItemsValidator(IEnumerable<ISchemaValidator> validators, IJsonValidationContextFactory contextFactory)
        {
            _validators = validators;
            _contextFactory = contextFactory;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            if (!_validators.Any())
            {
                return true;
            }

            int prefixItemIndex = 0;
            int schemaCount = _validators.Count();

            foreach (var item in context.Data.EnumerateArray())
            {
                if (prefixItemIndex >= schemaCount)
                    break;

                var validator = _validators.ElementAt(prefixItemIndex);
                var itemContext = _contextFactory.CreateContextForArrayItemFast(context, item);
                if (!validator.IsValid(itemContext))
                {
                    return false;
                }
                prefixItemIndex++;
            }

            return true;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                // If the instance is not an array, it's considered valid with respect to the prefixItems keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (!_validators.Any())
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (context is not IJsonValidationArrayContext arrayContext)
            {
                throw new InvalidOperationException("Array context is invalid");
            }

            var children = new List<ValidationResult>();
            int prefixItemIndex = 0;
            int arrayLength = context.Data.GetArrayLength();
            int schemaCount = _validators.Count();

            foreach (JsonElement item in context.Data.EnumerateArray())
            {
                if (prefixItemIndex >= schemaCount)
                    break;

                var validator = _validators.ElementAt(prefixItemIndex);
                var itemContext = _contextFactory.CreateContextForArrayItem(context, prefixItemIndex, item);
                var childKeywordPath = keywordLocation.Append(prefixItemIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                var itemValidationResult = validator.Validate(itemContext, childKeywordPath);
                children.Add(itemValidationResult);

                if (!itemValidationResult.IsValid)
                {
                    return ValidationResult.Invalid(instanceLocation, kwLocation, string.Concat("Item at index ", prefixItemIndex.ToString(System.Globalization.CultureInfo.InvariantCulture), " is invalid")) with { Children = children };
                }
                arrayContext.SetEvaluatedIndex(prefixItemIndex);
                prefixItemIndex++;
            }

            var result = ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };

            // Per spec: annotate with largest index validated, or true if array length <= schema count
            if (prefixItemIndex > 0)
            {
                // Avoid boxing by using separate dictionary creation paths
                if (arrayLength <= schemaCount)
                {
                    return result with
                    {
                        Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = true }
                    };
                }
                else
                {
                    return result with
                    {
                        Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = prefixItemIndex - 1 }
                    };
                }
            }

            return result;
        }
    }
}
