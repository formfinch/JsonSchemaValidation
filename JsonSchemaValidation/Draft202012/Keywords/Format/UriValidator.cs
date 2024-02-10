using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class UriValidator : IKeywordValidator
    {
        private readonly string keyword;
        private readonly UriValidationLogic uriValidation;

        public UriValidator(bool iriSupport = false, bool canBeRelative = false, bool isTemplate = false)
        {
            if (isTemplate)
            {
                keyword = "uri-template";
            }
            else
            {
                var uriOrIri = iriSupport ? "iri" : "uri";
                var suffix = canBeRelative ? "-relative" : string.Empty;
                keyword = $"format:{uriOrIri}{suffix}";
            }
            uriValidation = new UriValidationLogic(iriSupport, canBeRelative, isTemplate);
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.String)
            {
                return ValidationResult.Ok;
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Ok;
            }

            if (uriValidation.IsValidUri(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }
    }
}
