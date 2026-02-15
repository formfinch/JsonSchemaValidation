// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 3, Draft 4, Draft 6, Draft 7, Draft 2019-09
// Note: In Draft 2020-12, "additionalItems" was removed.
// Boolean true schema for additionalItems - all additional items are valid.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class AdditionalItemsTrueValidator : IKeywordValidator
    {
        private readonly int _tupleSize;

        public string Keyword => "additionalItems";

        public bool SupportsDirectValidation => true;

        public AdditionalItemsTrueValidator(int tupleSize)
        {
            _tupleSize = tupleSize;
        }

        public bool IsValid(JsonElement data) => true;

        public bool IsValid(IJsonValidationContext context) => true;

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            // Mark additional indices as evaluated
            if (context is IJsonValidationArrayContext arrayContext)
            {
                int length = context.Data.GetArrayLength();
                for (int idx = _tupleSize; idx < length; idx++)
                {
                    arrayContext.SetEvaluatedIndex(idx);
                }

                if (length > _tupleSize)
                {
                    return ValidationResult.Valid(instanceLocation, kwLocation) with
                    {
                        Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = true }
                    };
                }
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
