// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Validates uri, uri-reference, iri, iri-reference, and uri-template formats.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords.Format
{
    internal sealed class UriValidator : IKeywordValidator
    {
        private readonly string _formatName;
        private readonly UriValidationLogic uriValidation;

        public string Keyword => "format";

        public bool SupportsDirectValidation => true;

        public UriValidator(bool iriSupport = false, bool canBeRelative = false, bool isTemplate = false)
        {
            if (isTemplate)
            {
                _formatName = "uri-template";
            }
            else
            {
                var uriOrIri = iriSupport ? "iri" : "uri";
                var suffix = canBeRelative ? "-relative" : string.Empty;
                _formatName = $"{uriOrIri}{suffix}";
            }
            uriValidation = new UriValidationLogic(iriSupport, canBeRelative, isTemplate);
        }

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;
            var str = data.GetString();
            return str == null || uriValidation.IsValidUri(str);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
                return ValidationResult.Valid(instanceLocation, kwLocation);

            if (!IsValid(context.Data))
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value is not a valid {_formatName}");

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = _formatName }
            };
        }
    }
}
