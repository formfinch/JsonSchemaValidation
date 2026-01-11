// Draft behavior: Identical in Draft 2019-09, Draft 2020-12
// Note: contentSchema was added in Draft 2019-09.
// Annotation-only validator for contentSchema keyword.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft201909.Keywords
{
    /// <summary>
    /// Validator for the contentSchema keyword.
    /// This is an annotation-only keyword that produces an annotation
    /// but does not perform validation.
    /// </summary>
    internal sealed class ContentSchemaValidator : IKeywordValidator
    {
        private readonly JsonElement _schema;

        public string Keyword => "contentSchema";

        public bool SupportsDirectValidation => true;

        public bool IsValid(JsonElement data) => true;

        public bool IsValid(IJsonValidationContext context) => true;

        public ContentSchemaValidator(JsonElement schema)
        {
            _schema = schema;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?>(StringComparer.Ordinal) { ["contentSchema"] = _schema }
            };
        }
    }
}
