// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: In Draft 3/Draft 4, minimum is a number with optional exclusiveMinimum boolean modifier.
// Validates that numeric data is >= (or >) the minimum value.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class MinimumValidator : IKeywordValidator
    {
        private readonly double _minimum;
        private readonly bool _exclusive;

        public string Keyword => "minimum";

        public bool SupportsDirectValidation => true;

        public MinimumValidator(double minimum, bool exclusive)
        {
            _minimum = minimum;
            _exclusive = exclusive;
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Number)
                return true;

            var value = data.GetDouble();
            return _exclusive ? value > _minimum : value >= _minimum;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            var comparison = _exclusive ? "greater than" : "at least";
            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value must be {comparison} {_minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }
    }
}
