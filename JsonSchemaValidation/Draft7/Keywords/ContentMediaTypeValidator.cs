// Draft behavior: Identical in Draft 7, Draft 2019-09, Draft 2020-12
// Annotation-only validator for contentMediaType keyword.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft7.Keywords
{
    /// <summary>
    /// Validator for the contentMediaType keyword.
    /// This is an annotation-only keyword that produces an annotation
    /// but does not perform validation.
    /// </summary>
    internal sealed class ContentMediaTypeValidator : IKeywordValidator
    {
        private readonly string _mediaType;

        public string Keyword => "contentMediaType";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data) => true;

        public bool IsValid(IJsonValidationContext context) => true;

        public ContentMediaTypeValidator(string mediaType)
        {
            _mediaType = mediaType;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { ["contentMediaType"] = _mediaType }
            };
        }
    }
}
