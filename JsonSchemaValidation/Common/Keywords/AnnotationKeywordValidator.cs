// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Common.Keywords
{
    /// <summary>
    /// Generic annotation-only keyword validator.
    /// Always returns valid; emits the keyword's value as an annotation.
    /// Used for title, description, default, deprecated, readOnly, writeOnly, examples.
    /// </summary>
    internal sealed class AnnotationKeywordValidator : IKeywordValidator
    {
        private readonly string _keyword;
        private readonly object? _value;

        public string Keyword => _keyword;

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data) => true;

        public bool IsValid(IJsonValidationContext context) => true;

        public AnnotationKeywordValidator(string keyword, object? value)
        {
            _keyword = keyword;
            _value = value;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [_keyword] = _value }
            };
        }
    }
}
