// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft 3 behavior: type "any" matches any JSON value.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class TypeAnyValidator : IKeywordValidator
    {
        public static readonly TypeAnyValidator Instance = new();

        public string Keyword => "type";

        public bool SupportsDirectValidation => true;

        private TypeAnyValidator() { }

        public bool IsValid(JsonElement data) => true;

        public bool IsValid(IJsonValidationContext context) => true;

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            return ValidationResult.Valid(context.InstanceLocation.ToString(), keywordLocation.ToString());
        }
    }
}
