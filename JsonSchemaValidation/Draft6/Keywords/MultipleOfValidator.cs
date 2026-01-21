// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates that numeric data is a multiple of the specified divisor.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
{
    internal sealed class MultipleOfValidator : IKeywordValidator
    {
        private readonly double _divisor;

        public string Keyword => "multipleOf";

        public bool SupportsDirectValidation => true;

        public MultipleOfValidator(double divisor)
        {
            _divisor = divisor;
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Number)
                return true;

            double theValue = data.GetDouble();
            if (Math.Abs(theValue % _divisor) < double.Epsilon)
                return true;

            double quotient = theValue / _divisor;
            if (double.IsInfinity(quotient)
                && Math.Abs(theValue % 1) < double.Epsilon
                && Math.Abs(1.0 % _divisor) < double.Epsilon)
                return true;

            quotient = Math.Round((quotient + 0.000001) * 100) / 100.0;
            return Math.Abs(quotient - Math.Round(quotient)) < double.Epsilon;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value is not a multiple of {_divisor.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }
    }
}
