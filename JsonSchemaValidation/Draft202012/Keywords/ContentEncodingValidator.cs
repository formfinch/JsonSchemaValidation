using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Validator for the contentEncoding keyword.
    /// This is an annotation-only keyword that produces an annotation
    /// but does not perform validation.
    /// </summary>
    internal class ContentEncodingValidator : IKeywordValidator
    {
        private readonly string _encoding;

        public ContentEncodingValidator(string encoding)
        {
            _encoding = encoding;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var result = new ValidationResult();
            result.SetAnnotation("contentEncoding", _encoding);
            return result;
        }
    }
}
