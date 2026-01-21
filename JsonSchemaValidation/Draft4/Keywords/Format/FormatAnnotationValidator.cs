// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 2019-09, Draft 2020-12
// Annotation-only format validator - always returns valid.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft4.Keywords.Format
{
    /// <summary>
    /// Annotation-only format validator.
    /// Per Draft 2019-09 spec, the format keyword is annotation-only by default.
    /// This validator only produces an annotation and always returns valid.
    /// </summary>
    internal sealed class FormatAnnotationValidator : IKeywordValidator
    {
        private readonly string _format;

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data) => true;

        public bool IsValid(IJsonValidationContext context) => true;

        public FormatAnnotationValidator(string format)
        {
            _format = format;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { ["format"] = _format }
            };
        }
    }
}
