using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Validator for the contentSchema keyword.
    /// This is an annotation-only keyword that produces an annotation
    /// but does not perform validation.
    /// </summary>
    internal class ContentSchemaValidator : IKeywordValidator
    {
        private readonly JsonElement _schema;

        public ContentSchemaValidator(JsonElement schema)
        {
            _schema = schema;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var result = new ValidationResult();
            result.SetAnnotation("contentSchema", _schema);
            return result;
        }
    }
}
