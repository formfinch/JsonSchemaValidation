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
        private readonly bool iriSupport;
        private readonly bool canBeRelative;
        private readonly bool performTemplateExpansion;
        private readonly UriTemplateExpander? expander;

        public UriValidator(bool iriSupport = false, bool canBeRelative = false, bool isTemplate = false)
        {

            if (isTemplate)
            {
                iriSupport = false;
                canBeRelative = true;
                keyword = "uri-template";
            }
            else
            {
                var uriOrIri = iriSupport ? "iri" : "uri";
                var suffix = canBeRelative ? "-relative" : string.Empty;
                keyword = $"format:{uriOrIri}{suffix}";
            }

            this.iriSupport = iriSupport;
            this.canBeRelative = canBeRelative;
            performTemplateExpansion = isTemplate;
            expander = isTemplate ? (UriTemplateExpander?)new UriTemplateExpander() : null;
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

            if (IsValidUri(instanceString))
            {
                return ValidationResult.Ok;
            }

            return new ValidationResult(keyword);
        }

        private bool IsValidUri(string uri)
        {
            if(string.IsNullOrWhiteSpace(uri))
            {
                return false;
            }
            
            if(canBeRelative)
            {
                if (uri.StartsWith("//"))
                {
                    uri = "scheme:" + uri;
                }
                else if (!uri.Contains("://"))
                {
                    uri = "scheme://host" + uri;
                }
            }

            if(performTemplateExpansion)
            {
                uri = expander.ExpandTemplate(uri);
            }

            if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? validatedUri))
            {
                if (iriSupport
                    && !string.IsNullOrWhiteSpace(validatedUri.PathAndQuery)
                    && validatedUri.HostNameType != UriHostNameType.IPv6
                    && !string.IsNullOrWhiteSpace(validatedUri.Host)
                    && !string.IsNullOrWhiteSpace(validatedUri.IdnHost))
                {
                    var iri = uri.Replace(validatedUri.Host, validatedUri.IdnHost);
                    if (Uri.TryCreate(iri, UriKind.Absolute, out Uri? validatedIri))
                    {
                        validatedUri = validatedIri;
                    }
                }
                return validatedUri.IsWellFormedOriginalString();
            }
            return false;
        }
    }
}
