using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
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
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            var instanceString = context.Data.GetString();
            if (instanceString == null)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            if (uriValidation.IsValidUri(instanceString))
            {
                return ValidationResult.Valid(instanceLocation, kwLocation) with
                {
                    Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { [Keyword] = _formatName }
                };
            }

            return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value is not a valid {_formatName}");
        }
    }
}
