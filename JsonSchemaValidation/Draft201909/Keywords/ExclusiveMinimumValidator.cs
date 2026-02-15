// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 2019-09, Draft 2020-12 (numeric value)
// Note: In Draft 4-7, exclusiveMinimum was a boolean modifier for minimum.
// Starting with Draft 2019-09, it's a standalone numeric value.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    internal sealed class ExclusiveMinimumValidator : IKeywordValidator
    {
        private readonly double _minimum;

        public string Keyword => "exclusiveMinimum";

        public bool SupportsDirectValidation => true;

        public ExclusiveMinimumValidator(double minimum)
        {
            _minimum = minimum;
        }

        public bool IsValid(JsonElement data) =>
            data.ValueKind != JsonValueKind.Number || data.GetDouble() > _minimum;

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value must be greater than {_minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }
    }
}
