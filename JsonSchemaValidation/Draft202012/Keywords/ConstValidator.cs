// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Polyfills;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class ConstValidator : IKeywordValidator
    {
        private readonly JsonElement _expectedValue;

        public string Keyword => "const";

        public bool SupportsDirectValidation => true;

        public ConstValidator(JsonElement expectedValue)
        {
            _expectedValue = expectedValue;
        }

        public bool IsValid(JsonElement data) => JsonElementHelper.DeepEquals(_expectedValue, data);

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value must equal the const value");
        }
    }
}
