// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09
// Note: In Draft 2020-12, "additionalItems" was removed.
// Boolean false schema for additionalItems - no additional items allowed.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    internal sealed class AdditionalItemsFalseValidator : IKeywordValidator
    {
        private readonly int _tupleSize;

        public string Keyword => "additionalItems";

        public bool SupportsDirectValidation => true;

        public AdditionalItemsFalseValidator(int tupleSize)
        {
            _tupleSize = tupleSize;
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Array)
                return true;
            return data.GetArrayLength() <= _tupleSize;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            int length = context.Data.GetArrayLength();
            if (length > _tupleSize)
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Additional items not allowed. Array has {length.ToString(System.Globalization.CultureInfo.InvariantCulture)} items but only {_tupleSize.ToString(System.Globalization.CultureInfo.InvariantCulture)} are allowed");
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
