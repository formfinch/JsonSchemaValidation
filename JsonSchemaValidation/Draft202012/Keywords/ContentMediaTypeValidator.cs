using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Validator for the contentMediaType keyword.
    /// This is an annotation-only keyword that produces an annotation
    /// but does not perform validation.
    /// </summary>
    internal class ContentMediaTypeValidator : IKeywordValidator
    {
        private readonly string _mediaType;

        public ContentMediaTypeValidator(string mediaType)
        {
            _mediaType = mediaType;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var result = new ValidationResult();
            result.SetAnnotation("contentMediaType", _mediaType);
            return result;
        }
    }
}
