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
    internal sealed class BooleanFalseValidator : IKeywordValidator
    {
        public string Keyword => "";  // Boolean schema has no keyword name

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data) => false;

        public bool IsValid(IJsonValidationContext context) => false;

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            return ValidationResult.Invalid(context.InstanceLocation.ToString(), keywordLocation.ToString(), "Schema is false - all values are invalid");
        }
    }
}
