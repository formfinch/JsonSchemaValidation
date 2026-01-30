// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// The enum keyword validates that data equals one of the specified values.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft4.Keywords
{
    internal sealed class EnumValidator : IKeywordValidator
    {
        private readonly JsonElement _enumValuesElement;

        public string Keyword => "enum";

        public bool SupportsDirectValidation => true;

        public EnumValidator(JsonElement enumValuesElement)
        {
            _enumValuesElement = enumValuesElement;
        }

        public bool IsValid(JsonElement data)
        {
            var enumArray = _enumValuesElement.EnumerateArray();
            while (enumArray.MoveNext())
            {
                if (JsonElement.DeepEquals(enumArray.Current, data))
                    return true;
            }
            return false;
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (IsValid(context.Data))
                return ValidationResult.Valid(instanceLocation, kwLocation);

            return ValidationResult.Invalid(instanceLocation, kwLocation, "Value must be one of the enumerated values");
        }
    }
}
